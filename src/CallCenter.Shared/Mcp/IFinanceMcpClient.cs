using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

/// <summary>
/// MCP client for financial operations (refund, payment, etc.).
/// Production implementations should call the actual payment gateway.
/// </summary>
public interface IFinanceMcpClient
{
    /// <summary>Processes a refund for the given order.</summary>
    Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default);
}
