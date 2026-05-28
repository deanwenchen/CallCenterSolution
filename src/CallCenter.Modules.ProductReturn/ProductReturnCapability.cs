using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.ProductReturn;

/// <summary>
/// 商品退货能力。
/// </summary>
public sealed class ProductReturnCapability(IEnumerable<ICapabilityWorkflowRouteProvider> routeProviders) : ICapability
{
    public string Key => "ProductReturn";

    public CapabilityType Type => CapabilityType.Unknown;

    public async Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken)
                .ConfigureAwait(false);

            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        return new WorkflowSelection(
            route?.WorkflowName ?? "ProductReturnWorkflow",
            Type,
            route?.Reason ?? "Product return capability selected ProductReturnWorkflow.",
            CapabilityKey: Key);
    }
}
