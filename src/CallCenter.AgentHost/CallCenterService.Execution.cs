#pragma warning disable MAAI001
using CallCenter.Framework.Audit;
using CallCenter.Framework.Saga;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenterService 工作流执行层。
/// 主要作用：驱动工作流运行、处理 9 种 WorkflowEvent 类型、共享事件循环 DriveLoopAsync、
/// RunWorkflowAsync 启动新流程、ResumeWorkflowAsync 从断点恢复、Saga 补偿在 HandleEventAsync 内处理。
/// 迁移自 Program.cs 的 RunWorkflow / ResumeWorkflow，去重为共享的 DriveLoopAsync + HandleEventAsync。
/// </summary>
public partial class CallCenterService
{
    private enum EventResult { Continue, Return, Retry }

    /// <summary>Mutable execution state shared across DriveLoopAsync and HandleEventAsync.</summary>
    private sealed class ExecutionContext
    {
        public CheckpointInfo? LastCheckpoint;
        public bool NeedsOrderId;
        public RefundIntent? CurrentMessage;
    }

    /// <summary>
    /// 运行新工作流。替代 Program.cs 的 RunWorkflow 方法。
    /// 处理 RequestInfo/WorkflowOutput/SuperStepCompleted/WorkflowError/ExecutorFailed 事件，
    /// 以及 WorkflowStarted/ExecutorInvoked/ExecutorCompleted/WorkflowWarning 审计事件。
    /// 当需要订单号时（RefundSignal.NeedOrderId），自动重跑流程。
    /// </summary>
    public async Task<string> RunWorkflowAsync(string sessionId, RefundIntent initialMessage, CancellationToken ct)
    {
        var ctx = new ExecutionContext { CurrentMessage = initialMessage };

        while (true)
        {
            await using var run = await InProcessExecution.RunStreamingAsync(_refundWorkflow, ctx.CurrentMessage!, _checkpointManager, sessionId: sessionId, cancellationToken: ct).ConfigureAwait(false);

            var result = await DriveLoopAsync(sessionId, run, isResumeMode: false, ctx, ct).ConfigureAwait(false);
            if (result == EventResult.Return)
            {
                return ctx.CurrentMessage?.ToString() ?? string.Empty;
            }
            if (result == EventResult.Retry)
            {
                continue; // restart the while loop (re-run with updated currentMessage)
            }

            if (!ctx.NeedsOrderId) break;
        }

        return string.Empty;
    }

    /// <summary>
    /// 从断点恢复工作流。替代 Program.cs 的 ResumeWorkflow 方法。
    /// 从 session store 读取 lastCheckpoint，使用 ResumeStreamingAsync 恢复执行。
    /// RequestInfoEvent 处理更简单（不需要 orderId retry loop）。
    /// </summary>
    public async Task<string> ResumeWorkflowAsync(string sessionId, string userMessage, CancellationToken ct)
    {
        var checkpoint = await _sessionStore.GetAsync<CheckpointInfo>("lastCheckpoint", sessionId, ct).ConfigureAwait(false);
        if (checkpoint == null)
        {
            return "未找到断点记录，请重新启动流程";
        }

        await using var run = await InProcessExecution.ResumeStreamingAsync(_refundWorkflow, checkpoint, _checkpointManager, ct).ConfigureAwait(false);

        var ctx = new ExecutionContext { LastCheckpoint = checkpoint };

        var result = await DriveLoopAsync(sessionId, run, isResumeMode: true, ctx, ct).ConfigureAwait(false);
        if (result == EventResult.Return)
        {
            // Return value depends on what caused the return
            return ctx.LastCheckpoint?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// 共享事件循环。遍历 run.WatchStreamAsync() 的每个 WorkflowEvent，调用 HandleEventAsync 处理。
    /// 返回 EventResult 告知调用者是继续、返回还是重试。
    /// </summary>
    private async Task<EventResult> DriveLoopAsync(string sessionId, StreamingRun run, bool isResumeMode, ExecutionContext ctx, CancellationToken ct)
    {
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().WithCancellation(ct))
        {
            var result = await HandleEventAsync(sessionId, evt, run, ctx, isResumeMode, ct).ConfigureAwait(false);
            if (result != EventResult.Continue)
            {
                return result;
            }
        }
        return EventResult.Continue;
    }

    /// <summary>
    /// 处理单个工作流事件。处理 9 种事件类型（per CS-03）。
    /// 所有事件处理中的审计日志和 session store 调用都使用 sessionId 参数。
    /// </summary>
    private async Task<EventResult> HandleEventAsync(
        string sessionId,
        WorkflowEvent evt,
        StreamingRun run,
        ExecutionContext ctx,
        bool isResumeMode,
        CancellationToken ct)
    {
        switch (evt)
        {
            case RequestInfoEvent reqEvt:
            {
                await AuditTrailMiddleware.CaptureStepStart(_auditLogger, sessionId, "RequestInfo", reqEvt, ct).ConfigureAwait(false);

                if (reqEvt.Request.TryGetDataAs<RefundSignal>(out var signal) && signal == RefundSignal.NeedOrderId && !isResumeMode)
                {
                    Console.Write("请提供订单号: ");
                    var orderId = await _inputChannel.Reader.ReadAsync(ct).ConfigureAwait(false) ?? "";
                    await _sessionStore.SetAsync("pendingOrderId", orderId, sessionId, ct).ConfigureAwait(false);
                    ctx.CurrentMessage = ctx.CurrentMessage with { OrderId = orderId };
                    ctx.NeedsOrderId = true;
                    await run.SendResponseAsync(reqEvt.Request.CreateResponse(new RefundIntent(orderId, "U100"))).ConfigureAwait(false);
                }
                else
                {
                    var response = await HandleRequestAsync(reqEvt.Request, sessionId, ct).ConfigureAwait(false);
                    await run.SendResponseAsync(response).ConfigureAwait(false);
                }

                await AuditTrailMiddleware.CaptureStepEnd(_auditLogger, sessionId, "RequestInfo", reqEvt, ct).ConfigureAwait(false);
                return EventResult.Continue;
            }

            case WorkflowOutputEvent outputEvt:
            {
                Console.WriteLine($"\n[结果] {outputEvt.Data}");
                await AuditTrailMiddleware.CaptureStepEnd(_auditLogger, sessionId, "WorkflowOutput", outputEvt, ct).ConfigureAwait(false);

                if (ctx.LastCheckpoint != null)
                {
                    await _sessionStore.SetAsync("lastCheckpoint", ctx.LastCheckpoint, sessionId, ct).ConfigureAwait(false);
                }
                await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct).ConfigureAwait(false);

                if (_auditLogger != null)
                {
                    var verifyResult = await _auditLogger.VerifyChainAsync(sessionId, ct).ConfigureAwait(false);
                    Console.WriteLine($"\n[审计] {verifyResult.Message}");
                }

                return EventResult.Return;
            }

            case SuperStepCompletedEvent ssc:
            {
                if (ssc.CompletionInfo?.Checkpoint != null)
                {
                    ctx.LastCheckpoint = ssc.CompletionInfo.Checkpoint;
                    await _sessionStore.SetAsync("lastCheckpoint", ctx.LastCheckpoint, sessionId, ct).ConfigureAwait(false);
                }
                await AuditTrailMiddleware.CaptureStepEnd(_auditLogger, sessionId, "SuperStep", ssc, ct).ConfigureAwait(false);
                return EventResult.Continue;
            }

            case WorkflowErrorEvent errEvt:
            {
                var executorId = errEvt.Exception?.Source ?? "workflow";
                var reason = errEvt.Exception?.Message ?? "未知错误";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[错误] 工作流失败: {executorId} - {reason}");
                Console.ResetColor();

                await AuditTrailMiddleware.CaptureError(_auditLogger, sessionId, executorId, errEvt.Exception ?? new Exception(reason), ct).ConfigureAwait(false);

                // Saga compensation (per D-10-02)
                if (executorId == "ExecuteRefund")
                {
                    Console.WriteLine("\n[补偿] 检测到 ExecuteRefund 失败，触发 Saga 补偿...");
                    bool compensationTriggered = false;
                    try
                    {
                        var saga = new SagaBuilder()
                            .OnFailure("ExecuteRefund", async compensationCt =>
                            {
                                Console.WriteLine("[补偿] 执行补偿: 恢复优惠券...");
                                compensationTriggered = true;
                            })
                            .WithRetry(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));

                        await saga.ExecuteAsync(async _ => throw errEvt.Exception!, default).ConfigureAwait(false);

                        if (compensationTriggered)
                        {
                            Console.WriteLine("[补偿] 补偿完成");
                            await AuditTrailMiddleware.CaptureStepEnd(_auditLogger, sessionId, "SagaCompensation", new { Step = "ExecuteRefund", Compensation = "RestoreCoupon" }, ct).ConfigureAwait(false);
                        }
                    }
                    catch (SagaCompensationException sagaEx)
                    {
                        Console.WriteLine($"[补偿] 补偿失败: {sagaEx.Message}");
                        await AuditTrailMiddleware.CaptureError(_auditLogger, sessionId, "SagaCompensation", sagaEx, ct).ConfigureAwait(false);
                    }
                }

                await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct).ConfigureAwait(false);
                return EventResult.Return;
            }

            case ExecutorFailedEvent failEvt:
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[错误] 执行器失败: {failEvt.ExecutorId} - {failEvt.Data}");
                Console.ResetColor();

                await AuditTrailMiddleware.CaptureError(_auditLogger, sessionId, failEvt.ExecutorId ?? "unknown", new Exception($"Executor {failEvt.ExecutorId} failed: {failEvt.Data}"), ct).ConfigureAwait(false);
                await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct).ConfigureAwait(false);
                return EventResult.Return;
            }

            case WorkflowStartedEvent:
            {
                await _auditLogger.LogAsync("info", null, null, "Workflow started", sessionId, ct).ConfigureAwait(false);
                return EventResult.Continue;
            }

            case ExecutorInvokedEvent invokedEvt:
            {
                await _auditLogger.LogAsync("info", null, null, $"Executor invoked: {invokedEvt.ExecutorId}", sessionId, ct).ConfigureAwait(false);
                return EventResult.Continue;
            }

            case ExecutorCompletedEvent completedEvt:
            {
                await _auditLogger.LogAsync("info", null, null, $"Executor completed: {completedEvt.ExecutorId}", sessionId, ct).ConfigureAwait(false);
                return EventResult.Continue;
            }

            case WorkflowWarningEvent warningEvt:
            {
                await _auditLogger.LogAsync("warning", null, null, warningEvt.Data?.ToString() ?? "", sessionId, ct).ConfigureAwait(false);
                return EventResult.Continue;
            }

            default:
                return EventResult.Continue;
        }
    }
}
