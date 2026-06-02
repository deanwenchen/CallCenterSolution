using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 等待用户确认换货执行器（骨架）。
/// 主要作用：把订单信息发送给确认端口，等待用户确认或取消换货。
/// 当前未实现，仅保留扩展点。
/// </summary>
[SendsMessage(typeof(ConfirmExchangeRequest))]
internal sealed class WaitExchangeConfirmExecutor : Executor<ExchangeRuleResult, ConfirmExchangeRequest>
{
    public WaitExchangeConfirmExecutor() : base("WaitExchangeConfirm") { }

    public override ValueTask<ConfirmExchangeRequest> HandleAsync(ExchangeRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
