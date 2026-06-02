using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

/// <summary>
/// 财务服务 MCP 客户端接口。用于执行退款等资金操作。
/// 当前使用 MockFinanceService 模拟退款。
/// 生产环境应接入真实支付网关。
/// </summary>
public interface IFinanceMcpClient
{
    /// <summary>Processes a refund for the given order.</summary>
    Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default);
}
