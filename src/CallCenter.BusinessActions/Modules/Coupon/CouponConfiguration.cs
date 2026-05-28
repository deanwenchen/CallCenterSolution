using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Coupon;

/// <summary>
/// 优惠券模块的意图、路由和权限配置。
/// </summary>
public sealed class CouponConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([new("Coupon", IntentType.Coupon, ["coupon", "优惠券", "券"], 0.9)]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([new("Coupon", "Coupon", CapabilityType.Coupon, "Coupon -> Coupon")]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([new("Coupon", "CouponWorkflow", "Coupon -> CouponWorkflow")]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "CouponWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("CouponWorkflow", ["IssueCoupon", "SendNotification"], ["Coupon.Issue", "WeCom.SendMessage"])
            : null;

        return Task.FromResult(permission);
    }
}
