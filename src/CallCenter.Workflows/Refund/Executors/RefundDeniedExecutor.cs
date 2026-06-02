using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

/// <summary>
/// 退款拒绝。规则校验失败时的替代路径。输出拒绝原因，流程终止。
/// </summary>
[YieldsOutput(typeof(RefundNotification))]
internal sealed class RefundDeniedExecutor : Executor<RefundRuleResult>
{
    public RefundDeniedExecutor() : base("RefundDenied") { }

    public override async ValueTask HandleAsync(RefundRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.YieldOutputAsync(new RefundNotification($"退款被拒绝: {message.Reason}"), cancellationToken);
    }
}
