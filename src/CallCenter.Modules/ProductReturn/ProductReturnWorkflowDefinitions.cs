using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.ProductReturn;

/// <summary>
/// 商品退货模块自己的流程定义。
/// </summary>
public sealed class ProductReturnWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "ProductReturnWorkflow",
            CapabilityType.Unknown,
            [
                Step("GET_ORDER", "QueryOrder"),
                Step("CHECK_RETURN_RULE", "CheckProductReturnRule"),
                Step("WAIT_USER_CONFIRM", "WaitUserConfirm", humanInTheLoop: true),
                Step("CREATE_RETURN_ORDER", "CreateProductReturnOrder"),
                Step("SEND_NOTIFICATION", "SendNotification")
            ],
            [
                Edge("GET_ORDER", "CHECK_RETURN_RULE"),
                Edge("CHECK_RETURN_RULE", "WAIT_USER_CONFIRM"),
                Edge("WAIT_USER_CONFIRM", "CREATE_RETURN_ORDER"),
                Edge("CREATE_RETURN_ORDER", "SEND_NOTIFICATION")
            ],
            nameof(WorkflowState))
    ];

    private static WorkflowStepDefinition Step(
        string name,
        string businessActionName,
        string? compensationBusinessActionName = null,
        bool humanInTheLoop = false)
    {
        return new WorkflowStepDefinition(
            name,
            businessActionName,
            Timeout: TimeSpan.FromSeconds(30),
            MaxRetries: 3,
            compensationBusinessActionName,
            humanInTheLoop);
    }

    private static WorkflowEdgeDefinition Edge(string from, string to)
    {
        return new WorkflowEdgeDefinition(from, to, "previous.CanContinue");
    }
}
