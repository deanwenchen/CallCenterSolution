#pragma warning disable MAAI001
using CallCenter.Workflows.Refund;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenterService 入口层。
/// 主要作用：提供统一服务入口 ProcessAsync(sessionId, userMessage) → string，
/// 内部完成意图识别→工作流路由→执行/恢复→返回结果。
/// 整合 ProcessResult 的 dispatch 逻辑（从 Program.cs 主循环迁移而来）。
/// </summary>
public partial class CallCenterService
{
    /// <summary>
    /// 统一服务入口。接收 sessionId + userMessage，返回处理结果字符串。
    /// 内部完成：意图识别 → 工作流路由 → 执行/恢复 → 返回结果。
    /// 非业务意图返回问候语，不启动工作流。
    /// 会话超时时返回终止消息。
    /// </summary>
    public async Task<string> ProcessAsync(string sessionId, string userMessage, CancellationToken ct = default)
    {
        // 1. Route: determine what to do based on session state and user intent
        var result = await ResolveWorkflow(sessionId, userMessage, ct);

        // 2. Dispatch based on ProcessResult type
        switch (result)
        {
            case ResumeExistingResult resumeResult:
                // Wave 3: will call ResumeWorkflowAsync with sessionId
                // Currently stubbed — ResumeWorkflowAsync not yet implemented
                return await ResumeWorkflowAsync(sessionId, userMessage, resumeResult.Workflow, ct).ConfigureAwait(false);

            case TimeoutResult timeout:
                return timeout.Message;

            case IntentSwitchResult switchResult:
                return $"已终止 {switchResult.OldWorkflow} 流程，新意图 '{switchResult.NewIntent}' 暂未实现";

            case NoIntentResult noIntent:
                return noIntent.Response;

            case StartWorkflowResult start:
                return await RunWorkflowAsync(sessionId, (RefundIntent)start.InitialMessage, start.Workflow, ct).ConfigureAwait(false);

            default:
                return "系统处理异常，请重试。";
        }
    }

}
