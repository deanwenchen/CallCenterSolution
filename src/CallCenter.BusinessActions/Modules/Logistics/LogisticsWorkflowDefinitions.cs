using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Logistics;

/// <summary>
/// 物流模块的流程定义。
/// </summary>
public sealed class LogisticsWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "LogisticsWorkflow",
            CapabilityType.Logistics,
            [Step("QUERY_LOGISTICS", "QueryLogistics"), Step("SEND_NOTIFICATION", "SendNotification")],
            [Edge("QUERY_LOGISTICS", "SEND_NOTIFICATION")],
            nameof(WorkflowState))
    ];

    private static WorkflowStepDefinition Step(string name, string businessActionName)
    {
        return new WorkflowStepDefinition(name, businessActionName, TimeSpan.FromSeconds(30), 3);
    }

    private static WorkflowEdgeDefinition Edge(string from, string to)
    {
        return new WorkflowEdgeDefinition(from, to, "previous.CanContinue");
    }
}
