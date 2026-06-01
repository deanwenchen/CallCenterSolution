// TODO: PRD Section 7.5 — Audit Logger (auto-capture Workflow Step input/output, immutable store)
namespace CallCenter.Framework.Audit;

public static class AuditLogger
{
    // TODO: Implement structured audit logging
    // TODO: Implement immutable audit trail storage
    public static Task LogAsync(string executorId, object? input, object? output, CancellationToken ct = default)
    {
        // TODO: Write to immutable store
        return Task.CompletedTask;
    }
}
