using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Framework.EventBus;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(ExchangeNotification))]
internal sealed class ExchangeSendNotificationExecutor : Executor<CouponRestored, ExchangeNotification>
{
    private readonly IBusinessEventBus _eventBus;

    public ExchangeSendNotificationExecutor(IBusinessEventBus eventBus) : base("ExchangeSendNotification")
    {
        _eventBus = eventBus;
    }

    public ExchangeSendNotificationExecutor() : this(null!) { }

    public override ValueTask<ExchangeNotification> HandleAsync(CouponRestored message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
