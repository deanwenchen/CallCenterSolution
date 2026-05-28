using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Crm;

/// <summary>
/// CRM 能力，选择 CRM 流程。
/// </summary>
public sealed class CrmCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "Crm";

    public CapabilityType Type => CapabilityType.Crm;

    public async Task<WorkflowSelection> SelectWorkflowAsync(IntentResult intent, CapabilitySelection capability, SessionContext session, CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "CrmWorkflow", Type, route?.Reason ?? "Crm selected CrmWorkflow.", Key);
    }
}
