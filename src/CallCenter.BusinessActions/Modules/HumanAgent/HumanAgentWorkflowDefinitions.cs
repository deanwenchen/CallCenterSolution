using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.HumanAgent;

/// <summary>
/// 人工客服模块的流程定义。
/// </summary>
public sealed class HumanAgentWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "HumanHandoffWorkflow",
            CapabilityType.HumanAgent,
            [
                new WorkflowStepDefinition("SEARCH_KNOWLEDGE", "SearchKnowledge", TimeSpan.FromSeconds(30), 3),
                new WorkflowStepDefinition("HUMAN_HANDOFF", "HumanHandoff", TimeSpan.FromSeconds(30), 3, HumanInTheLoop: true)
            ],
            [new WorkflowEdgeDefinition("SEARCH_KNOWLEDGE", "HUMAN_HANDOFF", "previous.CanContinue")],
            nameof(WorkflowState))
    ];
}
