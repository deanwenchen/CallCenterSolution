using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Coupon;

/// <summary>
/// 优惠券能力，选择发券流程。
/// </summary>
public sealed class CouponCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "Coupon";

    public CapabilityType Type => CapabilityType.Coupon;

    public async Task<WorkflowSelection> SelectWorkflowAsync(IntentResult intent, CapabilitySelection capability, SessionContext session, CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken).ConfigureAwait(false);
            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(route?.WorkflowName ?? "CouponWorkflow", Type, route?.Reason ?? "Coupon selected CouponWorkflow.", Key);
    }
}
