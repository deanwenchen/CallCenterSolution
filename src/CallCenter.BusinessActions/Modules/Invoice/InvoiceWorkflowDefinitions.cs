using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Invoice;

/// <summary>
/// 发票模块的流程定义。
/// </summary>
public sealed class InvoiceWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "InvoiceWorkflow",
            CapabilityType.Invoice,
            [Step("CREATE_INVOICE", "CreateInvoice"), Step("SEND_NOTIFICATION", "SendNotification")],
            [Edge("CREATE_INVOICE", "SEND_NOTIFICATION")],
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
