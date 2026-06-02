using CallCenter.Shared.Models;

namespace CallCenter.Workflows.Exchange;

/// <summary>换货工作流输入 — 包含用户意图和可选订单号。</summary>
public record ExchangeIntent(string? OrderId, string? UserId);

/// <summary>GetExchangeOrderExecutor 内部信号，用于回传状态或请求信息。</summary>
public enum ExchangeSignal { Init, NeedOrderId, OrderFound, Ineligible, Cancelled }

/// <summary>GetExchangeOrderExecutor 输出 — 成功查询到的订单信息。</summary>
public record ExchangeOrderFound(OrderInfo Order);

/// <summary>CheckExchangeRuleExecutor 输出 — 包含换货是否合格、拒绝原因和订单信息。</summary>
public record ExchangeRuleResult(bool IsEligible, string? Reason, string? OrderId, string? ProductName);

/// <summary>WaitExchangeConfirmExecutor → ConfirmPort 的请求 — 向用户展示换货确认信息。</summary>
public record ConfirmExchangeRequest(string OrderId, string ProductName);

/// <summary>ConfirmPort → ExecuteExchangeExecutor 的响应 — 用户确认或取消换货。</summary>
public record UserConfirmation(bool Confirmed);

/// <summary>ExecuteExchangeExecutor 输出 — Result 为 null 表示用户取消了换货。</summary>
public record ExchangeExecuted(ExchangeResult? Result);

/// <summary>ExchangeRestoreCouponExecutor 输出 — 记录恢复的优惠券 ID。</summary>
public record CouponRestored(string? CouponId);

/// <summary>工作流最终输出 — 显示给用户的换货通知消息。</summary>
public record ExchangeNotification(string Message);

/// <summary>换货结果占位类型。v2 实现时补充真实字段。</summary>
public record ExchangeResult(string? Message);
