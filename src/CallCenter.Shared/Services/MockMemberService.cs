using CallCenter.Shared.Models;
using CallCenter.Shared.Mcp;

namespace CallCenter.Shared.Services;

/// <summary>
/// Mock 会员服务。
/// 主要作用：在没有真实会员系统时，模拟优惠券查询和恢复能力，支撑退款后的补偿流程。
/// 返回固定优惠券（CPN-2024，¥20 折扣），恢复操作始终成功。
/// </summary>
public class MockMemberService : IMemberMcpClient
{
    public Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult<CouponInfo?>(new CouponInfo(
            CouponId: "CPN-2024",
            UserId: userId,
            Discount: 20.00m,
            ExpiryDate: DateTime.Now.AddMonths(3)));
    }

    public Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
