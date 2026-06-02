using CallCenter.Shared.Models;

namespace CallCenter.Workflows.Refund;

/// <summary>退款工作流输入 — 包含用户意图和可选订单号。</summary>
public record RefundIntent(string? OrderId, string? UserId);

/// <summary>GetOrderExecutor 内部信号，用于向端口回传状态或请求信息。</summary>
public enum RefundSignal { Init, NeedOrderId, OrderFound, Ineligible, Cancelled }

/// <summary>GetOrderExecutor 输出 — 成功查询到的订单信息。</summary>
public record OrderFound(OrderInfo Order);

/// <summary>CheckRefundRuleExecutor 输出 — 包含退款是否合格、拒绝原因、可退金额。</summary>
public record RefundRuleResult(bool IsEligible, string? Reason, decimal RefundAmount, string? OrderId, string? ProductName);

/// <summary>WaitUserConfirmExecutor → ConfirmPort 的请求 — 向用户展示退款确认信息。</summary>
public record ConfirmRefundRequest(decimal Amount, string OrderId, string ProductName);

/// <summary>ConfirmPort → ExecuteRefundExecutor 的响应 — 用户确认或取消退款。</summary>
public record UserConfirmation(bool Confirmed);

/// <summary>ExecuteRefundExecutor 输出 — Result 为 null 表示用户取消了退款。</summary>
public record RefundExecuted(RefundResult? Result);

/// <summary>RestoreCouponExecutor 输出 — 记录恢复的优惠券 ID。</summary>
public record CouponRestored(string? CouponId);

/// <summary>工作流最终输出 — 显示给用户的退款通知消息。</summary>
public record RefundNotification(string Message);
