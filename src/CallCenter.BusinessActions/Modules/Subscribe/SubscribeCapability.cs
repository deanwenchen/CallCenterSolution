using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Subscribe;

/// <summary>
/// 订阅能力，选择订阅流程。
/// </summary>
public sealed class SubscribeCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "Subscribe";

    public CapabilityType Type => CapabilityType.Subscribe;

    public async Task<WorkflowSelection> SelectWorkflowAsync(IntentResult intent, CapabilitySelection capability, SessionContext session, CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "SubscribeWorkflow", Type, route?.Reason ?? "Subscribe selected SubscribeWorkflow.", Key);
    }
}
