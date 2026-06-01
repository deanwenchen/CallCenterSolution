using CallCenter.Shared.Models;

namespace CallCenter.Workflows.Refund;

// Initial workflow input
public record RefundIntent(string? OrderId, string? UserId);

// Signal enum for parameter loop back to InfoPort
public enum RefundSignal { Init, NeedOrderId, OrderFound, Ineligible, Cancelled }

// GetOrderExecutor output
public record OrderFound(OrderInfo Order);

// CheckRefundRuleExecutor output
public record RefundRuleResult(bool IsEligible, string? Reason, decimal RefundAmount, string? OrderId, string? ProductName);

// WaitUserConfirmExecutor → Port request
public record ConfirmRefundRequest(decimal Amount, string OrderId, string ProductName);

// Port → ExecuteRefundExecutor response (user confirmation)
public record UserConfirmation(bool Confirmed);

// ExecuteRefundExecutor output
public record RefundExecuted(RefundResult Result);

// RestoreCouponExecutor output
public record CouponRestored(string? CouponId);

// Final output
public record RefundNotification(string Message);
