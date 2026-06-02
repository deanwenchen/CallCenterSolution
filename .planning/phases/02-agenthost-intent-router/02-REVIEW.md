---
status: issues_found
phase: 02
date: 2026-06-01
---

# Phase 02: Code Review Report

**Reviewed:** 2026-06-01
**Depth:** standard (with cross-file analysis on key call chains)
**Files Reviewed:** 2
**Status:** issues_found

## Summary

Two source files were reviewed: `EntryPoint.cs` (intent recognition + session management) and `RefundSkill.cs` (refund skill definition). Three critical correctness bugs were found, including a timeout check that is structurally unreachable and a redundant AI call that wastes resources and risks inconsistent behavior. Several medium and low severity issues relate to hardcoded test data in production-facing code and missing cancellation token propagation.

## Critical Issues

### CR-01: Timeout check is structurally unreachable — 30-minute warning and 60-minute termination will never fire

**File:** `src/CallCenter.AgentHost/EntryPoint.cs:121-129`

In `ProcessAsync`, `lastActivity` is updated **before** `CheckTimeoutAsync` is called:

```csharp
// Line 122 — updates timestamp to NOW
await _sessionStore.SetAsync("lastActivity", DateTime.UtcNow, sessionId, ct);

// Line 125 — checks timeout against the value we just set
var timeoutResult = await CheckTimeoutAsync(sessionId, ct);
```

Inside `CheckTimeoutAsync` (line 95-98):
```csharp
var lastActivity = await GetLastActivityAsync(sessionId, ct);  // returns DateTime.UtcNow from line 122
var elapsed = DateTime.UtcNow - lastActivity.Value;             // always near-zero (milliseconds)
```

Because `lastActivity` is set to `DateTime.UtcNow` just milliseconds before the elapsed calculation, `elapsed` is always sub-second. The 30-minute and 60-minute thresholds are **never reachable**. The timeout mechanism is dead code.

**Fix:** Move the timeout check **before** updating `lastActivity`:

```csharp
public async Task<ProcessResult> ProcessAsync(
    string sessionId,
    string userMessage,
    Workflow refundWorkflow,
    CancellationToken ct = default)
{
    // 1. Check timeout FIRST — against the PREVIOUS activity timestamp
    var timeoutResult = await CheckTimeoutAsync(sessionId, ct);
    if (timeoutResult != null)
    {
        return timeoutResult;
    }

    // 2. Only update lastActivity AFTER confirming the session is not timed out
    await _sessionStore.SetAsync("lastActivity", DateTime.UtcNow, sessionId, ct);

    // ... rest of the method unchanged
}
```

### CR-02: Double intent recognition — redundant AI call with risk of inconsistent results

**File:** `src/CallCenter.AgentHost/EntryPoint.cs:136, 164`

When an active workflow exists, `RecognizeIntentAsync` is called on line 136 to check for intent switches. Later, on line 164 (the "no active workflow" branch), it is called **again** with the same `userMessage`:

```csharp
// Line 136 — first call (inside active workflow branch)
var intent = await RecognizeIntentAsync(userMessage, ct);

// ... intent handling for active workflow ...

// Line 164 — second call (reached when activeWorkflow is null/empty)
var newIntent = await RecognizeIntentAsync(userMessage, ct);
```

Each call invokes `_intentAgent.RunAsync(userMessage)` which makes an AI/LLM request. This means:
- **Wasted cost**: Two AI calls per request when no active workflow exists (the common first-turn case).
- **Inconsistency risk**: Non-deterministic AI responses could classify the same message differently between the two calls, producing contradictory behavior.

**Fix:** Compute intent once and reuse the result:

```csharp
public async Task<ProcessResult> ProcessAsync(
    string sessionId,
    string userMessage,
    Workflow refundWorkflow,
    CancellationToken ct = default)
{
    // 1. Timeout check first (see CR-01)
    var timeoutResult = await CheckTimeoutAsync(sessionId, ct);
    if (timeoutResult != null) return timeoutResult;

    // 2. Update activity
    await _sessionStore.SetAsync("lastActivity", DateTime.UtcNow, sessionId, ct);

    // 3. Recognize intent ONCE
    var intent = await RecognizeIntentAsync(userMessage, ct);
    var activeWorkflow = await GetActiveWorkflowAsync(sessionId, ct);

    // 4. Handle active workflow vs. new workflow using the SAME intent result
    if (!string.IsNullOrEmpty(activeWorkflow))
    {
        // ... use `intent` here (no second call)
    }

    if (intent == null || intent.Intent is "unknown" or "greeting")
    {
        // ... use `intent` here (no second call)
    }

    if (intent.Intent == "refund")
    {
        await SetActiveWorkflowAsync(sessionId, "RefundWorkflow", ct);
        return ProcessResult.StartWorkflow(new RefundIntent(intent.OrderId, "U100"));
    }

    return ProcessResult.NoIntent("...");
}
```

### CR-03: Hardcoded user ID "U100" in production-facing code paths

**File:** `src/CallCenter.AgentHost/EntryPoint.cs:179`, `src/CallCenter.AgentHost/Skills/RefundSkill.cs:33`

Both files hardcode `"U100"` as a fallback user ID:

```csharp
// EntryPoint.cs:179
return ProcessResult.StartWorkflow(new RefundIntent(newIntent.OrderId, "U100"));

// RefundSkill.cs:33
var orders = await client.GetRecentOrdersAsync(userId ?? "U100");
```

This is mock/test data from `MockOrderService.cs` (which seeds orders for `"U100"`). In production, this means:
- If `newIntent.OrderId` is null and no `UserId` is available, the workflow executes for user `"U100"` — potentially returning or refunding the wrong user's order.
- The `RefundSkill.GetRecentOrders` fallback silently queries orders for `"U100"` when `userId` is null, leaking another user's order data.

**Fix:** Do not default to a hardcoded user ID. Return an error or prompt when user identity is unavailable:

```csharp
// EntryPoint.cs:179 — require UserId, fail fast if missing
if (string.IsNullOrEmpty(newIntent.OrderId))
{
    // Could pass null and let the workflow handle it, or return NoIntent
    return ProcessResult.NoIntent("请提供订单号以便处理退款。");
}
// If UserId must come from session, resolve it from session store
var userId = await _sessionStore.GetAsync<string>("userId", sessionId, ct);
if (string.IsNullOrEmpty(userId))
{
    return ProcessResult.NoIntent("请先登录以处理退款。");
}
return ProcessResult.StartWorkflow(new RefundIntent(newIntent.OrderId, userId));

// RefundSkill.cs:33 — require userId, no fallback
if (string.IsNullOrEmpty(userId))
{
    return "{\"error\": \"User ID is required to fetch orders.\"}";
}
```

## Warnings

### WR-01: Cancellation token not propagated to MCP client calls in RefundSkill

**File:** `src/CallCenter.AgentHost/Skills/RefundSkill.cs:33, 45`

Both skill methods accept `IServiceProvider` but not `CancellationToken`, and the MCP client calls omit cancellation:

```csharp
// Line 33 — no CancellationToken parameter, no ct passed to client
var orders = await client.GetRecentOrdersAsync(userId ?? "U100");

// Line 45 — same issue
var result = await client.RefundAsync(orderId, amount);
```

The interfaces `IOrderMcpClient.GetRecentOrdersAsync` and `IFinanceMcpClient.RefundAsync` both support `CancellationToken ct = default`. Without passing a token, these calls cannot be cancelled if the caller needs to abort (e.g., user disconnects, timeout).

**Fix:** Add `CancellationToken` parameters to the skill methods and propagate them:

```csharp
[AgentSkillScript("get_recent_orders")]
[Description("获取用户最近的订单列表。")]
private static async Task<string> GetRecentOrders(
    string? userId,
    IServiceProvider sp,
    CancellationToken ct = default)
{
    var client = sp.GetRequiredService<IOrderMcpClient>();
    var orders = await client.GetRecentOrdersAsync(userId, ct);
    return JsonSerializer.Serialize(orders);
}

[AgentSkillScript("execute_refund")]
[Description("执行退款操作。")]
private static async Task<string> ExecuteRefund(
    string orderId,
    decimal amount,
    IServiceProvider sp,
    CancellationToken ct = default)
{
    var client = sp.GetRequiredService<IFinanceMcpClient>();
    var result = await client.RefundAsync(orderId, amount, ct);
    return JsonSerializer.Serialize(result);
}
```

### WR-02: `InMemorySessionStore.GetAsync<T>` silently swallows type mismatches

**File:** `src/CallCenter.Framework/Session/InMemorySessionStore.cs:10-18`

When the stored value type does not match the requested type `T`, the `is T` pattern match fails silently and returns `default`:

```csharp
if (value is T typed) return Task.FromResult<T?>(typed);
// Falls through to:
return Task.FromResult<T?>(default);
```

For example, if `"lastActivity"` was accidentally stored as a `string` instead of `DateTime`, `GetAsync<DateTime?>("lastActivity", ...)` would return `null` without any indication of the type mismatch. This makes debugging session state issues difficult.

**Fix:** At minimum, log a warning or throw when type mismatch occurs. For a production store, consider storing type metadata alongside values:

```csharp
if (value is T typed) return Task.FromResult<T?>(typed);

// Log or handle type mismatch
// For now, at least log via ILogger or throw InvalidOperationException
System.Diagnostics.Debug.WriteLine(
    $"Type mismatch in session store: key='{key}', stored type='{value.GetType().Name}', requested type='{typeof(T).Name}'");

return Task.FromResult<T?>(default);
```

### WR-03: `CheckTimeoutAsync` clears active workflow on 30-minute warning, then caller proceeds to start a new workflow

**File:** `src/CallCenter.AgentHost/EntryPoint.cs:106-109`

When elapsed >= 30 minutes, `CheckTimeoutAsync` clears the active workflow and returns a `TimeoutWarning`:

```csharp
if (elapsed.TotalMinutes >= 30)
{
    await ClearActiveWorkflowAsync(sessionId, ct);
    return ProcessResult.TimeoutWarning("Warning: 30 minutes of inactivity...");
}
```

The caller in `ProcessAsync` returns this warning immediately (line 127-129), so the user sees the warning but their current message is discarded — it is not processed. This means a user who returns after 35 minutes and sends "我要退款" gets only a timeout warning, not the start of a new refund workflow. The user must send the message **again**.

**Fix:** After returning the timeout warning, the caller should re-process the user message against a clean session state. Or, distinguish between the warning and termination behaviors more carefully:

```csharp
// In ProcessAsync, after timeout warning:
if (timeoutResult != null)
{
    var timeout = (TimeoutResult)timeoutResult;
    if (timeout.IsWarning)
    {
        // User's message is still valid — fall through to normal processing
        // (workflow is already cleared, so it will start fresh)
    }
    else
    {
        return timeoutResult; // Termination — discard user message
    }
}
```

## Info

### IN-01: Fragile markdown code fence stripping in StructuredOutputParser

**File:** `src/CallCenter.Framework/Parsing/StructuredOutputParser.cs:17-21`

The parser strips markdown code fences by splitting on newlines and looking for lines starting with "```":

```csharp
if (json.StartsWith("```"))
{
    var lines = json.Split('\n');
    json = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
}
```

If the AI response contains a JSON value with embedded "```" (e.g., a string field containing markdown), parsing terminates early and returns a truncated result. Also, responses with leading whitespace before "```" (e.g., "```json") will not be stripped.

**Fix:** Use a more robust extraction:

```csharp
// Extract content between first and last code fence
var start = json.IndexOf("```");
if (start >= 0)
{
    var firstNewline = json.IndexOf('\n', start);
    var end = json.LastIndexOf("```");
    if (firstNewline >= 0 && end > firstNewline)
    {
        json = json.Substring(firstNewline + 1, end - firstNewline - 1).Trim();
    }
}
```

### IN-02: `IntentResult.Intent` field accepts any string — no validation on unrecognized intent values

**File:** `src/CallCenter.AgentHost/EntryPoint.cs:13, 139-146, 150-159`

`IntentResult` is defined as `record IntentResult(string Intent, ...)`, where `Intent` can be any string the AI returns. The system prompt instructs the AI to return `"refund"|"greeting"|"unknown"`, but if the AI returns an unexpected value (e.g., `"hello"`, `"cancel_order"`), the code falls through to the "intent switch" branch (line 156-160) and clears the active workflow.

**Fix:** Consider adding a known-intent validation layer, or at least logging unrecognized intents for monitoring:

```csharp
var knownIntents = new[] { "refund", "greeting", "unknown" };
if (!knownIntents.Contains(intent.Intent))
{
    // Log warning, treat as unknown
    return ProcessResult.NoIntent("抱歉，我不太明白。你可以说'我要退款'来开始退款流程。");
}
```

---

_Reviewed: 2026-06-01_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
