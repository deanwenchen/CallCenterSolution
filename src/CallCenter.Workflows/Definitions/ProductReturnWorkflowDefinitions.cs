using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.Workflows.WorkflowDefinitionDsl;

namespace CallCenter.Workflows;

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
}
