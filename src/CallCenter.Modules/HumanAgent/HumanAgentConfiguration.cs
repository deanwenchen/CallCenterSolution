using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.HumanAgent;

/// <summary>
/// 人工客服模块的意图、路由和权限配置。
/// </summary>
public sealed class HumanAgentConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>(
        [
            new("HumanAgent", IntentType.HumanAgent, ["human", "agent", "人工", "客服"], 0.9)
        ]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>(
        [
            new("HumanAgent", "HumanAgent", CapabilityType.HumanAgent, "HumanAgent -> HumanAgent")
        ]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>(
        [
            new("HumanAgent", "HumanHandoffWorkflow", "HumanAgent -> HumanHandoffWorkflow")
        ]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "HumanHandoffWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("HumanHandoffWorkflow", ["SearchKnowledge", "HumanHandoff"], [])
            : null;

        return Task.FromResult(permission);
    }
}
