using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Coupon;

/// <summary>
/// 优惠券模块的流程定义。
/// </summary>
public sealed class CouponWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "CouponWorkflow",
            CapabilityType.Coupon,
            [Step("ISSUE_COUPON", "IssueCoupon"), Step("SEND_NOTIFICATION", "SendNotification")],
            [new WorkflowEdgeDefinition("ISSUE_COUPON", "SEND_NOTIFICATION", "previous.CanContinue")],
            nameof(WorkflowState))
    ];

    private static WorkflowStepDefinition Step(string name, string businessActionName)
    {
        return new WorkflowStepDefinition(name, businessActionName, TimeSpan.FromSeconds(30), 3);
    }
}
