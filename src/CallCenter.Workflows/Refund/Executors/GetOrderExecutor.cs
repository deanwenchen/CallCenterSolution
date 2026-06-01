using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

[SendsMessage(typeof(OrderFound))]
[SendsMessage(typeof(RefundSignal))]
internal sealed class GetOrderExecutor : Executor<RefundIntent>
{
    private readonly IOrderMcpClient _orderService;

    public GetOrderExecutor(IOrderMcpClient orderService) : base("GetOrder")
    {
        _orderService = orderService;
    }

    public GetOrderExecutor() : this(null!) { }

    public override async ValueTask HandleAsync(RefundIntent message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.OrderId))
        {
            await context.SendMessageAsync(RefundSignal.NeedOrderId, cancellationToken: cancellationToken);
            return;
        }

        var order = await _orderService.GetOrderAsync(message.OrderId, cancellationToken);
        if (order == null)
        {
            await context.SendMessageAsync(RefundSignal.NeedOrderId, cancellationToken: cancellationToken);
            return;
        }

        await context.QueueStateUpdateAsync("order", order, scopeName: "Refund", cancellationToken);
        await context.SendMessageAsync(new OrderFound(order), cancellationToken: cancellationToken);
    }
}
