using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.HumanAgent;

/// <summary>
/// 人工客服能力，选择人工接管流程。
/// </summary>
public sealed class HumanAgentCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "HumanAgent";

    public CapabilityType Type => CapabilityType.HumanAgent;

    public async Task<WorkflowSelection> SelectWorkflowAsync(IntentResult intent, CapabilitySelection capability, SessionContext session, CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "HumanHandoffWorkflow", Type, route?.Reason ?? "HumanAgent selected HumanHandoffWorkflow.", Key);
    }
}
