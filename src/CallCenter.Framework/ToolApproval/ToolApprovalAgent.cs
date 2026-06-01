using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.ToolApproval;

public interface IToolApprovalAgent
{
    Task<bool> IsApprovedAsync(string toolName, object arguments, string sessionId, CancellationToken ct = default);
}

public class DefaultToolApprovalAgent : IToolApprovalAgent
{
    public Task<bool> IsApprovedAsync(string toolName, object arguments, string sessionId, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
