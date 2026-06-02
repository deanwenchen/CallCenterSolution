using CallCenter.Shared.Models;
using CallCenter.Shared.Mcp;

namespace CallCenter.Shared.Services;

/// <summary>
/// Mock 财务服务。
/// 主要作用：在没有真实支付网关时，模拟退款成功结果，方便本地演示和工作流联调。
/// 始终返回成功退款结果，自动生成退款单号。
/// </summary>
public class MockFinanceService : IFinanceMcpClient
{
    public Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default)
    {
        return Task.FromResult(new RefundResult(
            RefundId: $"RF-{Guid.NewGuid():N}",
            OrderId: orderId,
            Amount: amount,
            Status: "success",
            RefundDate: DateTime.Now,
            Message: "退款已处理"));
    }
}
