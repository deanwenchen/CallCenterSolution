using CallCenter.Framework.EventBus;
using CallCenter.Shared.Models;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

/// <summary>
/// 发送退款通知。第六步。成功时发布事件并输出通知，取消时输出流程结束消息。
/// </summary>
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

        if (refundResult == null)
        {
            // Cancelled path — refund already notified in ExecuteRefundExecutor
            await context.YieldOutputAsync(new RefundNotification("退款流程已结束"), cancellationToken);
            return;
        }

        await _eventBus.PublishAsync(new RefundCompletedEvent(
            SessionId: "demo-session",
            UserId: "U100",
            OrderId: refundResult.OrderId,
            RefundAmount: refundResult.Amount), cancellationToken);

        await context.YieldOutputAsync(new RefundNotification($"退款 {refundResult.RefundId} 已处理完成，预计 3-5 个工作日到账"), cancellationToken);
    }
}
