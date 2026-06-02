using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

/// <summary>
/// 等待用户确认。第三步。向 ConfirmPort 发送确认请求，工作流暂停等待用户回复。
/// </summary>
[SendsMessage(typeof(ConfirmRefundRequest))]
internal sealed class WaitUserConfirmExecutor : Executor<RefundRuleResult>
{
    public WaitUserConfirmExecutor() : base("WaitUserConfirm") { }

    public override async ValueTask HandleAsync(RefundRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.SendMessageAsync(
            new ConfirmRefundRequest(message.RefundAmount, message.OrderId ?? "", message.ProductName ?? ""),
            cancellationToken: cancellationToken);
    }
}
