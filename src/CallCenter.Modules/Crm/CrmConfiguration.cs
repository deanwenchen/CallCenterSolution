using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Crm;

/// <summary>
/// CRM 模块的意图、路由和权限配置。
/// </summary>
public sealed class CrmConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([new("Crm", IntentType.Crm, ["crm", "tag", "标签"], 0.9)]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([new("Crm", "Crm", CapabilityType.Crm, "Crm -> Crm")]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([new("Crm", "CrmWorkflow", "Crm -> CrmWorkflow")]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "CrmWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("CrmWorkflow", ["AddCrmTag"], ["CRM.AddTag"])
            : null;

        return Task.FromResult(permission);
    }
}
