using CallCenter.Shared.Models;
using CallCenter.Shared.Mcp;

namespace CallCenter.Shared.Services;

/// <summary>
/// Mock 订单服务。
/// 主要作用：在没有真实订单系统时，提供固定订单数据，支撑退款/换货流程的本地演示和测试。
/// 内置三个订单覆盖不同退款场景：
///   A001 — 已签收、3 天内、可退（有优惠券）
///   A002 — 已签收、30 天、不可退（定制商品）
///   A003 — 已发货未签收、不可退
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
