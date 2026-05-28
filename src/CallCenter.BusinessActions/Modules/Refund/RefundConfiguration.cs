using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Refund;

/// <summary>
/// 退款模块的意图、路由和权限配置。
/// </summary>
public sealed class RefundConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>(
        [
            new("Refund", IntentType.Refund, ["refund", "退款"], 0.9)
        ]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>(
        [
            new("Refund", "Refund", CapabilityType.Refund, "Refund -> Refund")
        ]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>(
        [
            new("Refund", "RefundWorkflow", "Refund -> RefundWorkflow")
        ]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = workflowName switch
        {
            "RefundWorkflow" => new WorkflowPermissionDefinition(
                "RefundWorkflow",
                ["QueryOrder", "CheckRefundRule", "WaitUserConfirm", "ExecuteRefund", "RestoreCoupon", "SendNotification"],
                ["Order.GetOrder", "Finance.Refund", "Member.RestoreCoupon", "WeCom.SendMessage"]),
            "ManualRefundWorkflow" => new WorkflowPermissionDefinition(
                "ManualRefundWorkflow",
                ["QueryOrder", "CheckRefundRule", "WaitUserConfirm", "HumanHandoff"],
                ["Order.GetOrder"]),
            _ => null
        };

        return Task.FromResult(permission);
    }
}
