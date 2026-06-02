using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

/// <summary>
/// MCP client for member/coupon operations.
/// Used to restore coupons after successful refunds.
/// </summary>
public interface IMemberMcpClient
{
    /// <summary>Retrieves the user's active coupon, if any.</summary>
    Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default);

    /// <summary>Restores a previously-used coupon for the user.</summary>
    Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default);
}
