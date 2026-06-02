using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

/// <summary>
/// 退款规则校验执行器。第二步。
/// 规则：1) 超过 7 天退货期 → 拒绝  2) 订单未签收 → 拒绝  3) 定制商品 → 拒绝
/// 通过后：将退款金额保存到 state（若有优惠券则扣减 20 元），发送给 WaitUserConfirm 或 RefundDenied。
/// </summary>
internal sealed class CheckRefundRuleExecutor : Executor<OrderFound, RefundRuleResult>
{
    public CheckRefundRuleExecutor() : base("CheckRefundRule") { }

    public override ValueTask<RefundRuleResult> HandleAsync(OrderFound message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var order = message.Order;
        var daysSinceOrder = (DateTime.Now - order.OrderDate).Days;

        if (daysSinceOrder > 7)
            return ValueTask.FromResult(new RefundRuleResult(false, "超过 7 天退货期", 0, order.OrderId, order.ProductName));

        if (order.Status != "delivered")
            return ValueTask.FromResult(new RefundRuleResult(false, "订单未签收", 0, order.OrderId, order.ProductName));

        if (order.Category == "custom")
            return ValueTask.FromResult(new RefundRuleResult(false, "定制商品不可退", 0, order.OrderId, order.ProductName));

        var refundAmount = order.Amount;
        if (order.HasCoupon) refundAmount -= 20.00m;

        context.QueueStateUpdateAsync("refundAmount", refundAmount, scopeName: "Refund", cancellationToken);

        return ValueTask.FromResult(new RefundRuleResult(true, null, refundAmount, order.OrderId, order.ProductName));
    }
}
