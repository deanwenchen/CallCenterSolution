#pragma warning disable MAAI001
using CallCenter.Framework.Parsing;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenterService 路由层。
/// 主要作用：意图→工作流映射、活跃工作流检查、超时检测、统一路由分发。
/// 替代 EntryPoint 中的路由逻辑，使用 CallCenterService 的依赖字段。
/// </summary>
public partial class CallCenterService
{
    // ===== Session State Helpers =====

    /// <summary>获取当前会话的活跃工作流名称。</summary>
    public async Task<string?> GetActiveWorkflowAsync(string sessionId, CancellationToken ct = default) =>
        await _sessionStore.GetAsync<string>("activeWorkflow", sessionId, ct);

    /// <summary>设置当前会话的活跃工作流名称。</summary>
    public async Task SetActiveWorkflowAsync(string sessionId, string workflowName, CancellationToken ct = default) =>
        await _sessionStore.SetAsync("activeWorkflow", workflowName, scope: sessionId, ct: ct);

    /// <summary>清除当前会话的活跃工作流。</summary>
    public async Task ClearActiveWorkflowAsync(string sessionId, CancellationToken ct = default) =>
        await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct);

    /// <summary>获取当前会话的最后活动时间。</summary>
    public async Task<DateTime?> GetLastActivityAsync(string sessionId, CancellationToken ct = default) =>
        await _sessionStore.GetAsync<DateTime>("lastActivity", sessionId, ct);

    // ===== Timeout Detection =====

    /// <summary>
    /// 检查会话是否超时。
    /// 60 分钟无活动 → 终止会话（TimeoutTerminate）
    /// 30 分钟无活动 → 警告（TimeoutWarning）
    /// 无超时 → 返回 null
    /// </summary>
    public async Task<ProcessResult?> CheckTimeoutAsync(string sessionId, CancellationToken ct = default)
    {
        var lastActivity = await GetLastActivityAsync(sessionId, ct);
        if (lastActivity == null) return null;

        var elapsed = DateTime.UtcNow - lastActivity.Value;

        if (elapsed.TotalMinutes >= 60)
        {
            await ClearActiveWorkflowAsync(sessionId, ct);
            return ProcessResult.TimeoutTerminate("Session terminated due to 60 minutes of inactivity.");
        }

        if (elapsed.TotalMinutes >= 30)
        {
            return ProcessResult.TimeoutWarning("Warning: 30 minutes of inactivity. Session will terminate after 60 minutes of inactivity.");
        }

        return null;
    }

    // ===== Intent ↔ Workflow Mapping =====

    /// <summary>根据工作流名称反查对应的意图名称。</summary>
    public static string? GetIntentForWorkflow(string workflowName) =>
        workflowName switch
        {
            "RefundWorkflow" => "refund",
            "ExchangeWorkflow" => "exchange",
            _ => null,
        };

    /// <summary>根据意图名称反查对应的工作流名称。</summary>
    public static string? GetWorkflowForIntent(string intentName) =>
        intentName switch
        {
            "refund" => "RefundWorkflow",
            "exchange" => "ExchangeWorkflow",
            _ => null,
        };

    // ===== Core Routing =====

    /// <summary>
    /// 核心路由方法：根据用户消息和会话状态决定下一步操作。
    /// 返回 ProcessResult 告知调用者该启动、恢复、切换还是返回问候。
    /// </summary>
    public async Task<ProcessResult> ResolveWorkflow(string sessionId, string userMessage, CancellationToken ct)
    {
        // 1. Update lastActivity timestamp
        await _sessionStore.SetAsync("lastActivity", DateTime.UtcNow, scope: sessionId, ct: ct);

        // 2. Check timeout
        var timeoutResult = await CheckTimeoutAsync(sessionId, ct);
        if (timeoutResult != null) return timeoutResult;

        // 3. Check for active workflow
        var activeWorkflow = await GetActiveWorkflowAsync(sessionId, ct);

        if (!string.IsNullOrEmpty(activeWorkflow))
        {
            // Has active workflow — recognize user intent
            var intent = await RecognizeIntentAsync(userMessage, ct);

            // Non-business intent — don't interrupt current flow
            if (intent == null || !IntentRegistry.IsBusinessIntent(intent.Intent))
            {
                var response = intent?.Intent == "greeting"
                    ? "你好！有什么可以帮助你的？"
                    : "抱歉，我不太明白。";
                return ProcessResult.NoIntent(response);
            }

            // Intent matches current workflow — resume
            var currentIntent = GetIntentForWorkflow(activeWorkflow);
            if (currentIntent != null && intent.Intent == currentIntent)
            {
                return ProcessResult.ResumeExisting(_refundWorkflow);
            }

            // Intent switch — terminate old workflow
            await ClearActiveWorkflowAsync(sessionId, ct);
            return ProcessResult.IntentSwitch(activeWorkflow, intent.Intent);
        }

        // 4. No active workflow — recognize new intent
        var newIntent = await RecognizeIntentAsync(userMessage, ct);

        if (newIntent == null || !IntentRegistry.IsBusinessIntent(newIntent.Intent))
        {
            var response = newIntent?.Intent == "greeting"
                ? "你好！有什么可以帮助你的？"
                : "抱歉，我不太明白。";
            return ProcessResult.NoIntent(response);
        }

        // 5. Route: start corresponding workflow based on intent name
        switch (newIntent.Intent)
        {
            case "refund":
            {
                var orderId = newIntent.Parameters.GetValueOrDefault("OrderId");
                await SetActiveWorkflowAsync(sessionId, "RefundWorkflow", ct);
                return ProcessResult.StartWorkflow(new RefundIntent(orderId, "U100"), _refundWorkflow);
            }
            // Add new intents here:
            // case "exchange":
            //     var orderId2 = newIntent.Parameters.GetValueOrDefault("OrderId");
            //     await SetActiveWorkflowAsync(sessionId, "ExchangeWorkflow", ct);
            //     return ProcessResult.StartWorkflow(new ExchangeIntent(orderId2, "U100"), _exchangeWorkflow);
        }

        // Intent not matched — return error
        return ProcessResult.NoIntent("抱歉，无法启动该流程。");
    }

    // ===== Intent Recognition Helper (delegates to field) =====

    private async Task<IntentResult?> RecognizeIntentAsync(string userMessage, CancellationToken ct)
    {
        try
        {
            var intentAgent = _agentFactory.CreateIntentAgent(_skillsProvider);
            var response = await intentAgent.RunAsync(userMessage, cancellationToken: ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response?.Text))
                return null;

            var parser = new StructuredOutputParser<IntentResult>();
            return parser.Parse(response.Text);
        }
        catch
        {
            return null;
        }
    }
}
