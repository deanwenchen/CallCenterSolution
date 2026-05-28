using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 本地内存配置。后面接后台时，只需要替换这些 provider。
/// </summary>
public sealed class InMemoryCallCenterConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<WorkflowPermissionDefinition?>(null);
    }
}
