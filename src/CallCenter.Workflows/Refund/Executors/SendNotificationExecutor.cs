using CallCenter.Framework.EventBus;
using CallCenter.Shared.Models;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

[YieldsOutput(typeof(RefundNotification))]
internal sealed class SendNotificationExecutor : Executor<CouponRestored>
{
    private readonly IBusinessEventBus _eventBus;

    public SendNotificationExecutor(IBusinessEventBus eventBus) : base("SendNotification")
    {
        _eventBus = eventBus;
    }

    public SendNotificationExecutor() : this(null!) { }

    public override async ValueTask HandleAsync(CouponRestored message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var refundResult = await context.ReadStateAsync<RefundResult>("refundResult", scopeName: "Refund", cancellationToken);

        await _eventBus.PublishAsync(new RefundCompletedEvent(
            SessionId: "demo-session",
            UserId: "U100",
            OrderId: refundResult?.OrderId ?? "unknown",
            RefundAmount: refundResult?.Amount ?? 0), cancellationToken);

        await context.YieldOutputAsync(new RefundNotification($"退款 {refundResult?.RefundId ?? "REF-xxx"} 已处理完成，预计 3-5 个工作日到账"), cancellationToken);
    }
}
