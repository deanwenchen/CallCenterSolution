using CallCenter.Shared.Models;
using CallCenter.Shared.Mcp;

namespace CallCenter.Shared.Services;

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
