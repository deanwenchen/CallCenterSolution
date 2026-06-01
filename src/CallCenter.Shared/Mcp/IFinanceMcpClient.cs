using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

public interface IFinanceMcpClient
{
    Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default);
}
