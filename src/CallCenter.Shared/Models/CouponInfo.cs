namespace CallCenter.Shared.Models;

public record CouponInfo(
    string CouponId,
    string UserId,
    decimal Discount,
    DateTime ExpiryDate);
