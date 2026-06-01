using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

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
