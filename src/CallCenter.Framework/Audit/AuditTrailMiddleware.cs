using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.Audit;

/// <summary>
/// Static audit capture helper — called from ConsoleDemo event loop,
/// not a real MAF middleware (MAF has no middleware concept for workflows).
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
