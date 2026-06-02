namespace CallCenter.Shared.Models;

/// <summary>
/// Represents a coupon associated with a user.
/// Restored via IMemberMcpClient after a successful refund (compensation step).
/// </summary>
public record CouponInfo(
    string CouponId,
    string UserId,
    decimal Discount,
    DateTime ExpiryDate);
