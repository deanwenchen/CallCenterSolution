using CallCenter.Shared.Models;

namespace CallCenter.Workflows.Exchange;

// Initial workflow input
public record ExchangeIntent(string? OrderId, string? UserId);

// Signal enum for parameter loop back to InfoPort
public enum ExchangeSignal { Init, NeedOrderId, OrderFound, Ineligible, Cancelled }

// GetExchangeOrderExecutor output
public record ExchangeOrderFound(OrderInfo Order);

// CheckExchangeRuleExecutor output
public record ExchangeRuleResult(bool IsEligible, string? Reason, string? OrderId, string? ProductName);

// WaitExchangeConfirmExecutor → Port request
public record ConfirmExchangeRequest(string OrderId, string ProductName);

// Port → ExecuteExchangeExecutor response (user confirmation)
public record UserConfirmation(bool Confirmed);

// ExecuteExchangeExecutor output (Result is null when user cancelled)
public record ExchangeExecuted(ExchangeResult? Result);

// RestoreCouponExecutor output
public record CouponRestored(string? CouponId);

// Final output
public record ExchangeNotification(string Message);

// Placeholder result type (v2 will define actual fields)
public record ExchangeResult(string? Message);
