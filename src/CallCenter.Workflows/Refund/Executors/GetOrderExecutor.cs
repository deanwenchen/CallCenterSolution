using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

/// <summary>
/// 订单查询执行器。退款流程第一步。
/// 有订单号且存在时保存订单到 state 并发送 OrderFound；
/// 无订单号或不存在时发送 NeedOrderId 信号给 InfoPort 追问用户。
/// </summary>
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
        // First check if order ID was provided via session store (for dynamic prompt scenarios)
        var storedOrderId = await context.ReadStateAsync<string>("pendingOrderId", scopeName: "Refund", cancellationToken);
        if (string.IsNullOrWhiteSpace(message.OrderId) && !string.IsNullOrWhiteSpace(storedOrderId))
        {
            message = message with { OrderId = storedOrderId };
            await context.QueueStateUpdateAsync<string?>("pendingOrderId", null, scopeName: "Refund", cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(message.OrderId))
        {
            // Signal that we need order ID — main loop will prompt and set pendingOrderId in session store,
            // then re-invoke the workflow with the same RefundIntent.
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
