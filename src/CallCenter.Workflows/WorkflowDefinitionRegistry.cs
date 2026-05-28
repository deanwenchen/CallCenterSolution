using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Workflows;

public sealed class WorkflowDefinitionRegistry : IWorkflowDefinitionRegistry
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions;

    public WorkflowDefinitionRegistry()
    {
        WorkflowDefinition[] definitions =
        [
            CreateRefundWorkflow("RefundWorkflow", manualApproval: false),
            CreateRefundWorkflow("ManualRefundWorkflow", manualApproval: true),
            new(
                "LogisticsWorkflow",
                CapabilityType.Logistics,
                [
                    Step("QUERY_LOGISTICS", "QueryLogistics"),
                    Step("SEND_NOTIFICATION", "SendNotification")
                ],
                [Edge("QUERY_LOGISTICS", "SEND_NOTIFICATION")],
                nameof(WorkflowState)),
            new(
                "CrmWorkflow",
                CapabilityType.Crm,
                [Step("ADD_CRM_TAG", "AddCrmTag")],
                [],
                nameof(WorkflowState)),
            new(
                "HumanHandoffWorkflow",
                CapabilityType.HumanAgent,
                [Step("HUMAN_HANDOFF", "HumanHandoff", humanInTheLoop: true)],
                [],
                nameof(WorkflowState))
        ];

        _definitions = definitions.ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);
    }

    public WorkflowDefinition Get(string workflowName)
    {
        if (_definitions.TryGetValue(workflowName, out WorkflowDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Workflow '{workflowName}' is not registered.");
    }

    public IReadOnlyCollection<WorkflowDefinition> GetAll() => _definitions.Values;

    private static WorkflowDefinition CreateRefundWorkflow(string name, bool manualApproval)
    {
        List<WorkflowStepDefinition> steps =
        [
            Step("GET_ORDER", "QueryOrder"),
            Step("CHECK_REFUND_RULE", "CheckRefundRule"),
            Step("WAIT_USER_CONFIRM", "WaitUserConfirm", humanInTheLoop: true)
        ];

        if (!manualApproval)
        {
            steps.Add(Step("EXECUTE_REFUND", "ExecuteRefund", compensationBusinessActionName: "RestoreCoupon"));
            steps.Add(Step("RESTORE_COUPON", "RestoreCoupon"));
            steps.Add(Step("SEND_NOTIFICATION", "SendNotification"));
        }
        else
        {
            steps.Add(Step("HUMAN_HANDOFF", "HumanHandoff", humanInTheLoop: true));
        }

        List<WorkflowEdgeDefinition> edges =
        [
            Edge("GET_ORDER", "CHECK_REFUND_RULE"),
            Edge("CHECK_REFUND_RULE", "WAIT_USER_CONFIRM"),
            Edge("WAIT_USER_CONFIRM", manualApproval ? "HUMAN_HANDOFF" : "EXECUTE_REFUND")
        ];

        if (!manualApproval)
        {
            edges.Add(Edge("EXECUTE_REFUND", "RESTORE_COUPON"));
            edges.Add(Edge("RESTORE_COUPON", "SEND_NOTIFICATION"));
        }

        return new WorkflowDefinition(name, CapabilityType.Refund, steps, edges, nameof(WorkflowState));
    }

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
