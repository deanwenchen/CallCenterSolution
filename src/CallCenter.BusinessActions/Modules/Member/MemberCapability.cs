using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Member;

/// <summary>
/// 会员能力，选择会员查询流程。
/// </summary>
public sealed class MemberCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "Member";

    public CapabilityType Type => CapabilityType.Member;

    public async Task<WorkflowSelection> SelectWorkflowAsync(IntentResult intent, CapabilitySelection capability, SessionContext session, CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "MemberWorkflow", Type, route?.Reason ?? "Member selected MemberWorkflow.", Key);
    }
}
