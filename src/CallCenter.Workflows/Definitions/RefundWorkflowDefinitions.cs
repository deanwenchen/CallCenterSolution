using CallCenter.Domain;
using static CallCenter.Workflows.WorkflowDefinitionDsl;

namespace CallCenter.Workflows;

/// <summary>
/// 退款相关流程。
/// </summary>
internal static class RefundWorkflowDefinitions
{
    public static WorkflowDefinition StandardRefund()
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

    public static WorkflowDefinition ManualRefund()
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
}
