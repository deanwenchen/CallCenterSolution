using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(ExchangeNotification))]
internal sealed class ExchangeDeniedExecutor : Executor<ExchangeRuleResult, ExchangeNotification>
{
    public ExchangeDeniedExecutor() : base("ExchangeDenied") { }

    public override ValueTask<ExchangeNotification> HandleAsync(ExchangeRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
