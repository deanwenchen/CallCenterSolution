using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Framework.EventBus;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 换货通知执行器（骨架）。
/// 主要作用：在换货流程结束后对外输出通知，后续可接事件总线、消息通知等渠道。
/// 当前未实现，仅保留扩展点。
/// </summary>
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
