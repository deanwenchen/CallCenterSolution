using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

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
