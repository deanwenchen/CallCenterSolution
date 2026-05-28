using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Invoice;

/// <summary>
/// 发票模块的意图、路由和权限配置。
/// </summary>
public sealed class InvoiceConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>([new("Invoice", IntentType.Invoice, ["invoice", "发票"], 0.9)]);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>([new("Invoice", "Invoice", CapabilityType.Invoice, "Invoice -> Invoice")]);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>([new("Invoice", "InvoiceWorkflow", "Invoice -> InvoiceWorkflow")]);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = string.Equals(workflowName, "InvoiceWorkflow", StringComparison.OrdinalIgnoreCase)
            ? new WorkflowPermissionDefinition("InvoiceWorkflow", ["CreateInvoice", "SendNotification"], ["Invoice.Create", "WeCom.SendMessage"])
            : null;

        return Task.FromResult(permission);
    }
}
