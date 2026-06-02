namespace CallCenter.Shared.Models;

/// <summary>
/// 用户优惠券信息。退款成功后由 RestoreCouponExecutor 恢复。
/// 包含优惠券 ID、用户 ID、折扣金额和过期日期。
/// </summary>
public record CouponInfo(
    string CouponId,
    string UserId,
    decimal Discount,
    DateTime ExpiryDate);
