#pragma warning disable MAAI001
using System.Text.Json;
using CallCenter.Framework.Audit;
using CallCenter.Framework.Saga;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenterService 流式输出层。
/// ProcessStreamingAsync + DriveStreamingAsync + SSE 序列化。
/// 为前端提供实时工作流事件推送能力。
/// </summary>
public partial class CallCenterService
{
    /// <summary>
    /// SSE 流式处理入口。接收 sessionId + userMessage，yield return SSE 格式字符串。
    /// 内部完成：意图识别 → 工作流路由 → 流式事件循环 → 返回 SSE 字符串序列。
    /// </summary>
    public async IAsyncEnumerable<string> ProcessStreamingAsync(
        string sessionId,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Route: determine what to do based on session state and user intent
        var result = await ResolveWorkflow(sessionId, userMessage, ct).ConfigureAwait(false);

        // 2. Dispatch based on ProcessResult type
        switch (result)
        {
            case StartWorkflowResult start:
            {
                await using var run = await InProcessExecution.RunStreamingAsync(
                    _refundWorkflow, start.InitialMessage, _checkpointManager,
                    sessionId: sessionId, cancellationToken: ct).ConfigureAwait(false);

                await foreach (var sse in DriveStreamingAsync(sessionId, run, isResumeMode: false, ct).ConfigureAwait(false))
                {
                    yield return sse;
                }
                yield break;
            }

            case ResumeExistingResult resumeResult:
            {
                var checkpoint = await _sessionStore.GetAsync<CheckpointInfo>("lastCheckpoint", sessionId, ct).ConfigureAwait(false);
                if (checkpoint == null)
                {
                    yield return SerializeEventSse(new WorkflowErrorEvent(new Exception("No checkpoint found")));
                    yield break;
                }

                await using var run = await InProcessExecution.ResumeStreamingAsync(
                    _refundWorkflow, checkpoint, _checkpointManager, ct).ConfigureAwait(false);

                await foreach (var sse in DriveStreamingAsync(sessionId, run, isResumeMode: true, ct).ConfigureAwait(false))
                {
                    yield return sse;
                }
                yield break;
            }

            case TimeoutResult timeout:
            {
                var expiredEvent = $"data: {{\"type\":\"SessionExpired\",\"data\":{{\"reason\":\"60 minutes of inactivity\"}}}}\n\n";
                yield return expiredEvent;
                yield break;
            }

            case IntentSwitchResult switchResult:
            {
                yield return SerializeEventSse(new WorkflowWarningEvent($"Intent switched from {switchResult.OldWorkflow} to {switchResult.NewIntent}"));
                yield break;
            }

            case NoIntentResult noIntent:
            {
                yield return SerializeEventSse(new WorkflowOutputEvent(noIntent.Response, "NoIntent"));
                yield break;
            }

            default:
            {
                yield return SerializeEventSse(new WorkflowErrorEvent(new Exception("Unknown process result")));
                yield break;
            }
        }
    }

    /// <summary>
    /// 流式事件循环。遍历 run.WatchStreamAsync() 的每个 WorkflowEvent，
    /// 调用 HandleEventAsync 处理审计/会话/Saga，然后 yield return SSE 字符串。
    /// 与 DriveLoopAsync 结构相同，但将 Console.WriteLine 替换为 SSE yield return。
    /// </summary>
    private async IAsyncEnumerable<string> DriveStreamingAsync(
        string sessionId,
        StreamingRun run,
        bool isResumeMode,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var ctx = new ExecutionContext();

        await foreach (WorkflowEvent evt in run.WatchStreamAsync().WithCancellation(ct).ConfigureAwait(false))
        {
            var result = await HandleEventAsync(sessionId, evt, run, ctx, isResumeMode, ct).ConfigureAwait(false);
            yield return SerializeEventSse(evt);
            if (result != EventResult.Continue)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// 将任意 WorkflowEvent 序列化为 SSE 格式字符串。
    /// 格式: data: {"type":"EventTypeName","data":{...}}\n\n
    /// 无信封包装，直接序列化（per D-14-02, D-14-03）。
    /// </summary>
    internal static string SerializeEventSse(WorkflowEvent evt)
    {
        var typeName = evt.GetType().Name;
        // Strip "Event" suffix per SSE format convention (e.g., WorkflowStartedEvent → WorkflowStarted)
        if (typeName.EndsWith("Event"))
            typeName = typeName[..^5];
        var json = JsonSerializer.Serialize(evt, evt.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return $"data: {{\"type\":\"{typeName}\",\"data\":{json}}}\n\n";
    }

    /// <summary>
    /// 清除指定会话的所有数据（惰性清理用）。
    /// </summary>
    public Task ClearSessionScopeAsync(string sessionId, CancellationToken ct = default) =>
        _sessionStore.ClearScopeAsync(sessionId, ct);
}
