using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Member;

/// <summary>
/// 会员模块的意图、路由和权限配置。
/// </summary>
public sealed class MemberConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([new("Member", IntentType.Member, ["member", "points", "会员", "积分"], 0.9)]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([new("Member", "Member", CapabilityType.Member, "Member -> Member")]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([new("Member", "MemberWorkflow", "Member -> MemberWorkflow")]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "MemberWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("MemberWorkflow", ["QueryMember"], ["Member.GetMember"])
            : null;

        return Task.FromResult(permission);
    }
}
