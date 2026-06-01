using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(ExchangeRuleResult))]
internal sealed class CheckExchangeRuleExecutor : Executor<ExchangeOrderFound, ExchangeRuleResult>
{
    public CheckExchangeRuleExecutor() : base("CheckExchangeRule") { }

    public override ValueTask<ExchangeRuleResult> HandleAsync(ExchangeOrderFound message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
