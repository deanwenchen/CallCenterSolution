#pragma warning disable MAAI001
using CallCenter.Framework.Session;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

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
                return await ResumeWorkflowAsync(sessionId, userMessage, ct);

            case TimeoutResult timeout:
                return timeout.Message;

            case IntentSwitchResult switchResult:
                return $"已终止 {switchResult.OldWorkflow} 流程，新意图 '{switchResult.NewIntent}' 暂未实现";

            case NoIntentResult noIntent:
                return noIntent.Response;

            case StartWorkflowResult start:
                // Wave 3: will call RunWorkflowAsync with sessionId
                // Currently stubbed — RunWorkflowAsync not yet implemented
                return await RunWorkflowAsync(sessionId, start.InitialMessage, ct);

            default:
                return "系统处理异常，请重试。";
        }
    }

    /// <summary>
    /// 运行新工作流。
    /// Wave 3 实现 — 当前为占位。
    /// </summary>
    private async Task<string> RunWorkflowAsync(string sessionId, RefundIntent initialMessage, CancellationToken ct)
    {
        // TODO Wave 3: Implement full workflow execution
        // - Create execution context with audit, checkpoint, saga
        // - Drive workflow through event loop
        // - Handle RequestInfoEvent, WorkflowOutputEvent, WorkflowErrorEvent
        // - Return final result string
        throw new NotImplementedException("RunWorkflowAsync will be implemented in Wave 3 (Execution.cs)");
    }

    /// <summary>
    /// 从断点恢复工作流。
    /// Wave 3 实现 — 当前为占位。
    /// </summary>
    private async Task<string> ResumeWorkflowAsync(string sessionId, string userMessage, CancellationToken ct)
    {
        // TODO Wave 3: Implement workflow resume from checkpoint
        // - Load lastCheckpoint from session store
        // - Resume workflow execution from checkpoint
        // - Inject userMessage into waiting RequestPort
        // - Handle events same as RunWorkflowAsync
        // - Return final result string
        throw new NotImplementedException("ResumeWorkflowAsync will be implemented in Wave 3 (Execution.cs)");
    }
}
