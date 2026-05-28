using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Subscribe;

/// <summary>
/// 订阅模块的意图、路由和权限配置。
/// </summary>
public sealed class SubscribeConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([new("Subscribe", IntentType.Subscribe, ["subscribe", "subscription", "订阅"], 0.9)]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([new("Subscribe", "Subscribe", CapabilityType.Subscribe, "Subscribe -> Subscribe")]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([new("Subscribe", "SubscribeWorkflow", "Subscribe -> SubscribeWorkflow")]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "SubscribeWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("SubscribeWorkflow", ["UpdateSubscription", "SendNotification"], ["Subscribe.Update", "WeCom.SendMessage"])
            : null;

        return Task.FromResult(permission);
    }
}
