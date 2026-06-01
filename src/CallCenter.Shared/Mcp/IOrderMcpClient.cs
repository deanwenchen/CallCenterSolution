using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

public interface IOrderMcpClient
{
    Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default);
    Task<List<OrderInfo>> GetRecentOrdersAsync(string userId, CancellationToken ct = default);
}
