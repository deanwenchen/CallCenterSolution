using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Refund;

/// <summary>
/// 退款模块的流程定义。
/// </summary>
public sealed class RefundWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        StandardRefund(),
        ManualRefund()
    ];

    private static WorkflowDefinition StandardRefund()
    {
        return new WorkflowDefinition(
            "RefundWorkflow",
            CapabilityType.Refund,
            [
                Step("GET_ORDER", "QueryOrder"),
                Step("CHECK_REFUND_RULE", "CheckRefundRule"),
                Step("WAIT_USER_CONFIRM", "WaitUserConfirm", humanInTheLoop: true),
                Step("EXECUTE_REFUND", "ExecuteRefund", compensationBusinessActionName: "RestoreCoupon"),
                Step("RESTORE_COUPON", "RestoreCoupon"),
                Step("SEND_NOTIFICATION", "SendNotification")
            ],
            [
                Edge("GET_ORDER", "CHECK_REFUND_RULE"),
                Edge("CHECK_REFUND_RULE", "WAIT_USER_CONFIRM"),
                Edge("WAIT_USER_CONFIRM", "EXECUTE_REFUND"),
                Edge("EXECUTE_REFUND", "RESTORE_COUPON"),
                Edge("RESTORE_COUPON", "SEND_NOTIFICATION")
            ],
            nameof(WorkflowState));
    }

    private static WorkflowDefinition ManualRefund()
    {
        return new WorkflowDefinition(
            "ManualRefundWorkflow",
            CapabilityType.Refund,
            [
                Step("GET_ORDER", "QueryOrder"),
                Step("CHECK_REFUND_RULE", "CheckRefundRule"),
                Step("WAIT_USER_CONFIRM", "WaitUserConfirm", humanInTheLoop: true),
                Step("HUMAN_HANDOFF", "HumanHandoff", humanInTheLoop: true)
            ],
            [
                Edge("GET_ORDER", "CHECK_REFUND_RULE"),
                Edge("CHECK_REFUND_RULE", "WAIT_USER_CONFIRM"),
                Edge("WAIT_USER_CONFIRM", "HUMAN_HANDOFF")
            ],
            nameof(WorkflowState));
    }

    private static WorkflowStepDefinition Step(
        string name,
        string businessActionName,
        string? compensationBusinessActionName = null,
        bool humanInTheLoop = false)
    {
        return new WorkflowStepDefinition(name, businessActionName, TimeSpan.FromSeconds(30), 3, compensationBusinessActionName, humanInTheLoop);
    }

    private static WorkflowEdgeDefinition Edge(string from, string to)
    {
        return new WorkflowEdgeDefinition(from, to, "previous.CanContinue");
    }
}
