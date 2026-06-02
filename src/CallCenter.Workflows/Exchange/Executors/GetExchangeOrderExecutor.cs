using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 换货订单查询执行器（骨架）。
/// 主要作用：作为换货流程第一步，负责根据订单号查询订单，并在缺少订单号时触发追问。
/// 当前未实现，仅保留扩展点。
/// </summary>
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
