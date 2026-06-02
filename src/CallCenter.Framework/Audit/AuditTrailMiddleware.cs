using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.Audit;

/// <summary>
/// 审计追踪中间件。ConsoleDemo 事件循环中的静态审计捕获辅助方法。
/// 在每一步开始/结束/出错时记录日志到 AuditLogger。
/// 注意：这不是真正的 MAF 中间件（MAF 工作流没有中间件概念），
/// 只是在事件循环中手动调用的辅助方法。
/// </summary>
public static class AuditTrailMiddleware
{
    public static async Task CaptureStepStart(
        AuditLogger logger,
        string sessionId,
        string executorId,
        object input,
        CancellationToken ct = default)
    {
        await logger.LogAsync(executorId, input, null, "step_start", sessionId, ct);
    }

    public static async Task CaptureStepEnd(
        AuditLogger logger,
        string sessionId,
        string executorId,
        object output,
        CancellationToken ct = default)
    {
        await logger.LogAsync(executorId, null, output, "step_end", sessionId, ct);
    }

    public static async Task CaptureError(
        AuditLogger logger,
        string sessionId,
        string executorId,
        Exception ex,
        CancellationToken ct = default)
    {
        await logger.LogAsync(executorId, null, new { ex.Message, ex.StackTrace }, "error", sessionId, ct);
    }
}
