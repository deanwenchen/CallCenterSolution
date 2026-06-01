using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(ConfirmExchangeRequest))]
internal sealed class WaitExchangeConfirmExecutor : Executor<ExchangeRuleResult, ConfirmExchangeRequest>
{
    public WaitExchangeConfirmExecutor() : base("WaitExchangeConfirm") { }

    public override ValueTask<ConfirmExchangeRequest> HandleAsync(ExchangeRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
