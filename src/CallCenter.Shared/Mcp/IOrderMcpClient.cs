using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

/// <summary>
/// 订单服务 MCP 客户端接口。用于查询订单信息。
/// 当前使用 MockOrderService 模拟数据（A001 可退、A002 超期、A003 未签收）。
/// 生产环境应接入真实订单管理系统。
/// </summary>
public interface IOrderMcpClient
{
    /// <summary>Retrieves a specific order by ID.</summary>
    Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>Retrieves all recent orders for a given user.</summary>
    Task<List<OrderInfo>> GetRecentOrdersAsync(string userId, CancellationToken ct = default);
}
