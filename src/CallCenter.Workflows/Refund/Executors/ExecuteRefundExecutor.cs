using CallCenter.Shared.Mcp;
using CallCenter.Shared.Models;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

[SendsMessage(typeof(RefundExecuted))]
[SendsMessage(typeof(RefundSignal))]
internal sealed class ExecuteRefundExecutor : Executor<UserConfirmation, RefundExecuted>
{
    private readonly IFinanceMcpClient _financeService;

    public ExecuteRefundExecutor(IFinanceMcpClient financeService) : base("ExecuteRefund")
    {
        _financeService = financeService;
    }

    public ExecuteRefundExecutor() : this(null!) { }

    public override async ValueTask<RefundExecuted> HandleAsync(UserConfirmation message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!message.Confirmed)
        {
            await context.YieldOutputAsync(new RefundNotification("退款已取消"), cancellationToken);
            return new RefundExecuted(null);
        }

        var order = await context.ReadStateAsync<OrderInfo>("order", scopeName: "Refund", cancellationToken);
        var refundAmount = await context.ReadStateAsync<decimal>("refundAmount", scopeName: "Refund", cancellationToken);

        if (order == null) throw new InvalidOperationException("Order not found in state");

        var result = await _financeService.RefundAsync(order.OrderId, refundAmount, cancellationToken);

        await context.QueueStateUpdateAsync("refundResult", result, scopeName: "Refund", cancellationToken);

        return new RefundExecuted(result);
    }
}
