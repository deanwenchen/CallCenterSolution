using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

/// <summary>
/// MCP client for order operations.
/// Production implementations should call the actual order management system.
/// </summary>
public interface IOrderMcpClient
{
    /// <summary>Retrieves a specific order by ID.</summary>
    Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>Retrieves all recent orders for a given user.</summary>
    Task<List<OrderInfo>> GetRecentOrdersAsync(string userId, CancellationToken ct = default);
}
