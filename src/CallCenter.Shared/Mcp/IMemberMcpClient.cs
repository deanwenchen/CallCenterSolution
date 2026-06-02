using CallCenter.Shared.Models;

namespace CallCenter.Shared.Mcp;

/// <summary>
/// 会员服务 MCP 客户端接口。用于用户优惠券操作。
/// 退款成功后，RestoreCouponExecutor 调用此接口恢复用户优惠券。
/// 当前使用 MockMemberService 模拟数据。
/// 生产环境应接入真实会员系统。
/// </summary>
public interface IMemberMcpClient
{
    /// <summary>Retrieves the user's active coupon, if any.</summary>
    Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default);

    /// <summary>Restores a previously-used coupon for the user.</summary>
    Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default);
}
