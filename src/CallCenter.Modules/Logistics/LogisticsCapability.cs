using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Logistics;

/// <summary>
/// 物流能力，选择物流查询流程。
/// </summary>
public sealed class LogisticsCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "Logistics";

    public CapabilityType Type => CapabilityType.Logistics;

    public async Task<WorkflowSelection> SelectWorkflowAsync(IntentResult intent, CapabilitySelection capability, SessionContext session, CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "LogisticsWorkflow", Type, route?.Reason ?? "Logistics selected LogisticsWorkflow.", Key);
    }
}
