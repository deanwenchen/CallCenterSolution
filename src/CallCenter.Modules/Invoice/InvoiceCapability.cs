using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Invoice;

/// <summary>
/// 发票能力，选择发票流程。
/// </summary>
public sealed class InvoiceCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "Invoice";

    public CapabilityType Type => CapabilityType.Invoice;

    public async Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "InvoiceWorkflow", Type, route?.Reason ?? "Invoice selected InvoiceWorkflow.", Key);
    }
}
