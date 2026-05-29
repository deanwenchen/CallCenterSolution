using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Logistics;

/// <summary>
/// 物流模块的意图、路由和权限配置。
/// </summary>
public sealed class LogisticsConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([new("Logistics", IntentType.Logistics, ["logistics", "tracking", "物流", "快递"], 0.65)]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([new("Logistics", "Logistics", CapabilityType.Logistics, "Logistics -> Logistics")]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([new("Logistics", "LogisticsWorkflow", "Logistics -> LogisticsWorkflow")]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "LogisticsWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("LogisticsWorkflow", ["QueryLogistics", "SendNotification"], ["Logistics.Query", "WeCom.SendMessage"])
            : null;

        return Task.FromResult(permission);
    }
}
