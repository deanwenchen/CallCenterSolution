using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

[YieldsOutput(typeof(RefundNotification))]
internal sealed class RefundDeniedExecutor : Executor<RefundRuleResult>
{
    public RefundDeniedExecutor() : base("RefundDenied") { }

    public override async ValueTask HandleAsync(RefundRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.YieldOutputAsync(new RefundNotification($"退款被拒绝: {message.Reason}"), cancellationToken);
    }
}
