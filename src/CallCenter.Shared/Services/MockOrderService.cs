using CallCenter.Shared.Models;
using CallCenter.Shared.Mcp;

namespace CallCenter.Shared.Services;

/// <summary>
/// Mock implementation of IOrderMcpClient for demo/testing.
/// Contains three hardcoded orders covering different refund scenarios:
///   A001 — delivered, within 7 days, refundable (has coupon)
///   A002 — delivered, over 30 days, not refundable (custom item)
///   A003 — shipped (not yet delivered), not refundable
/// </summary>
public class MockOrderService : IOrderMcpClient
{
    private static readonly Dictionary<string, OrderInfo> _orders = new()
    {
        ["A001"] = new("A001", "U100", "蓝牙耳机", 299.00m, DateTime.Now.AddDays(-3), "delivered", "electronics", HasCoupon: true),
        ["A002"] = new("A002", "U100", "定制T恤", 159.00m, DateTime.Now.AddDays(-30), "delivered", "custom"),
        ["A003"] = new("A003", "U100", "手机壳", 39.00m, DateTime.Now.AddDays(-1), "shipped", "electronics"),
    };

    public Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        return Task.FromResult(_orders.GetValueOrDefault(orderId));
    }

    public Task<List<OrderInfo>> GetRecentOrdersAsync(string userId, CancellationToken ct = default)
    {
        var orders = _orders.Values.Where(o => o.UserId == userId).ToList();
        return Task.FromResult(orders);
    }
}
