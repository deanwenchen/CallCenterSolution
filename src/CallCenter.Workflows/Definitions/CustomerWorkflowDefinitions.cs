using CallCenter.Domain;
using static CallCenter.Workflows.WorkflowDefinitionDsl;

namespace CallCenter.Workflows;

/// <summary>
/// 客服通用流程。
/// </summary>
internal static class CustomerWorkflowDefinitions
{
    public static WorkflowDefinition Logistics()
    {
        return new WorkflowDefinition(
            "LogisticsWorkflow",
            CapabilityType.Logistics,
            [
                Step("QUERY_LOGISTICS", "QueryLogistics"),
                Step("SEND_NOTIFICATION", "SendNotification")
            ],
            [Edge("QUERY_LOGISTICS", "SEND_NOTIFICATION")],
            nameof(WorkflowState));
    }

    public static WorkflowDefinition Crm()
    {
        return new WorkflowDefinition(
            "CrmWorkflow",
            CapabilityType.Crm,
            [Step("ADD_CRM_TAG", "AddCrmTag")],
            [],
            nameof(WorkflowState));
    }

    public static WorkflowDefinition Invoice()
    {
        return new WorkflowDefinition(
            "InvoiceWorkflow",
            CapabilityType.Invoice,
            [
                Step("CREATE_INVOICE", "CreateInvoice"),
                Step("SEND_NOTIFICATION", "SendNotification")
            ],
            [Edge("CREATE_INVOICE", "SEND_NOTIFICATION")],
            nameof(WorkflowState));
    }

    public static WorkflowDefinition Subscribe()
    {
        return new WorkflowDefinition(
            "SubscribeWorkflow",
            CapabilityType.Subscribe,
            [
                Step("UPDATE_SUBSCRIPTION", "UpdateSubscription"),
                Step("SEND_NOTIFICATION", "SendNotification")
            ],
            [Edge("UPDATE_SUBSCRIPTION", "SEND_NOTIFICATION")],
            nameof(WorkflowState));
    }

    public static WorkflowDefinition Member()
    {
        return new WorkflowDefinition(
            "MemberWorkflow",
            CapabilityType.Member,
            [Step("QUERY_MEMBER", "QueryMember")],
            [],
            nameof(WorkflowState));
    }

    public static WorkflowDefinition Coupon()
    {
        return new WorkflowDefinition(
            "CouponWorkflow",
            CapabilityType.Coupon,
            [
                Step("ISSUE_COUPON", "IssueCoupon"),
                Step("SEND_NOTIFICATION", "SendNotification")
            ],
            [Edge("ISSUE_COUPON", "SEND_NOTIFICATION")],
            nameof(WorkflowState));
    }

    public static WorkflowDefinition HumanHandoff()
    {
        return new WorkflowDefinition(
            "HumanHandoffWorkflow",
            CapabilityType.HumanAgent,
            [
                Step("SEARCH_KNOWLEDGE", "SearchKnowledge"),
                Step("HUMAN_HANDOFF", "HumanHandoff", humanInTheLoop: true)
            ],
            [Edge("SEARCH_KNOWLEDGE", "HUMAN_HANDOFF")],
            nameof(WorkflowState));
    }
}
