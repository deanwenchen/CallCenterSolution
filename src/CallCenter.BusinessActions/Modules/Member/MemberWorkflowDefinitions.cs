using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Member;

/// <summary>
/// 会员模块的流程定义。
/// </summary>
public sealed class MemberWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "MemberWorkflow",
            CapabilityType.Member,
            [new WorkflowStepDefinition("QUERY_MEMBER", "QueryMember", TimeSpan.FromSeconds(30), 3)],
            [],
            nameof(WorkflowState))
    ];
}
