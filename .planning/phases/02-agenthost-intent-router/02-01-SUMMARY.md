---
phase: 02
plan: 01
subsystem: AgentHost
tags: [entrypoint, intent-recognition, session-management]
dependency_graph:
  requires: [01-foundation (InMemorySessionStore, StructuredOutputParser, RefundMessages)]
  provides: [IR-01, IR-02, IR-03]
  affects: [03-01 (timeout/intent-switch), 03-02 (ConsoleDemo)]
tech_stack:
  added: [Microsoft.Agents.AI, Microsoft.Agents.AI.Workflows, OpenAI, Microsoft.Extensions.AI]
  patterns: [ChatClientAgent, AIAgent, StructuredOutputParser, InMemorySessionStore]
key_files:
  created: [src/CallCenter.AgentHost/EntryPoint.cs]
  modified: []
decisions:
  - D-43~D-68: Phase 2 context decisions honored (DashScope Qwen, IntentResult structure, Resume deferred to Phase 3)
  - Added timeout checking (CheckTimeoutAsync) and intent switch detection (IntentSwitchResult) beyond original plan scope
metrics:
  duration: pre-implemented
  completed: "2026-06-01"
---

# Phase 02 Plan 01: EntryPoint — LLM 意图识别 + 活跃工作流管理 Summary

## 一句话总结

创建 EntryPoint.cs，实现基于 DashScope Qwen LLM 的意图识别、InMemorySessionStore 活跃工作流管理、以及区分类型的 ProcessResult 路由决策入口。

## 任务执行情况

### 任务 1.1：创建 EntryPoint.cs

**状态：已完成**（代码已在 commit `6536f88` 中实现）

实现内容：
- `record IntentResult(string Intent, string? Workflow, string? OrderId)` — LLM 意图解析结果
- `class EntryPoint` — 构造函数注入 `IChatClient` + `InMemorySessionStore`
- `RecognizeIntentAsync` — 使用 `ChatClientAgent` + `StructuredOutputParser<IntentResult>` 解析 LLM 响应
- `GetActiveWorkflowAsync` / `SetActiveWorkflowAsync` / `ClearActiveWorkflowAsync` — 基于 InMemorySessionStore 的会话管理
- `ProcessAsync` — 主入口方法，检查活跃工作流 → 意图识别 → 返回类型化 ProcessResult
- `record ProcessResult` 及子类型：`ResumeExistingResult`、`StartWorkflowResult(RefundIntent)`、`NoIntentResult(string)`
- 额外实现（超出原计划但属于正确扩展）：`TimeoutResult`、`IntentSwitchResult`、`CheckTimeoutAsync`、`GetLastActivityAsync`

### 任务 1.2：验证编译

**状态：已完成**

```
dotnet build src/CallCenter.AgentHost/CallCenter.AgentHost.csproj
→ 0 errors, 0 warnings
```

## Deviations from Plan

### Auto-added Missing Functionality

**Rule 2 — 超时检查与意图切换检测**
- **Found during:** 代码已存在于 prior commit `8ec7e40` / `23ebe3b`
- **Issue:** 原计划只包含基本意图识别和工作流管理，缺少会话超时和意图切换处理
- **Fix:** EntryPoint 已扩展包含 `CheckTimeoutAsync`（30分钟警告/60分钟终止）和 `IntentSwitchResult`（意图切换时清除旧工作流）
- **Files modified:** `src/CallCenter.AgentHost/EntryPoint.cs`
- **Commit:** `8ec7e40`, `23ebe3b`

## Known Stubs

| Stub | File | Line | Reason |
|------|------|------|--------|
| `ProcessResult.ResumeExisting()` returns empty result | EntryPoint.cs | ~52 | Phase 3 实现完整 Resume 机制（D-54~D-56） |
| `ProcessResult.StartWorkflow(new RefundIntent(orderId, "U100"))` | EntryPoint.cs | ~179 | UserId 硬编码为 "U100"，后续从 MCP 或上下文获取 |

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag:session-hijack | EntryPoint.cs | sessionId 作为字符串传递，无认证验证 — Demo 阶段可接受，生产需 JWT |
| threat_flag:llm-injection | EntryPoint.cs | userMessage 直接传递给 LLM，无输入过滤 — Safety Pipeline (FW-03) 将缓解 |

## Self-Check: PASSED

- [x] `src/CallCenter.AgentHost/EntryPoint.cs` exists
- [x] `dotnet build` succeeds: 0 errors, 0 warnings
- [x] Commit `6536f88` contains EntryPoint.cs initial implementation
- [x] All plan acceptance criteria met
