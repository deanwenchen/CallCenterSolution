using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(ExchangeOrderFound))]
internal sealed class GetExchangeOrderExecutor : Executor<ExchangeSignal, ExchangeOrderFound>
{
    private readonly IOrderMcpClient _orderService;

    public GetExchangeOrderExecutor(IOrderMcpClient orderService) : base("GetExchangeOrder")
    {
        _orderService = orderService;
    }

    public GetExchangeOrderExecutor() : this(null!) { }

    public override ValueTask<ExchangeOrderFound> HandleAsync(ExchangeSignal message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
