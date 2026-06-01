using CallCenter.Shared.Models;
using CallCenter.Shared.Mcp;

namespace CallCenter.Shared.Services;

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
