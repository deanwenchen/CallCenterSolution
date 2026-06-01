using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

public interface IMemberMcpClient
{
    Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default);
    Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default);
}
