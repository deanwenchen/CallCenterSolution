using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Subscribe;

/// <summary>
/// 订阅模块的流程定义。
/// </summary>
public sealed class SubscribeWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "SubscribeWorkflow",
            CapabilityType.Subscribe,
            [Step("UPDATE_SUBSCRIPTION", "UpdateSubscription"), Step("SEND_NOTIFICATION", "SendNotification")],
            [new WorkflowEdgeDefinition("UPDATE_SUBSCRIPTION", "SEND_NOTIFICATION", "previous.CanContinue")],
            nameof(WorkflowState))
    ];

    private static WorkflowStepDefinition Step(string name, string businessActionName)
    {
        return new WorkflowStepDefinition(name, businessActionName, TimeSpan.FromSeconds(30), 3);
    }
}
