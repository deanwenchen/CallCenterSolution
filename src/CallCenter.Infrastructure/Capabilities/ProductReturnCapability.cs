using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 商品退货能力。
/// </summary>
public sealed class ProductReturnCapability(ICapabilityWorkflowRouteProvider routeProvider) : ICapability
{
    public string Key => "ProductReturn";

    public CapabilityType Type => CapabilityType.Unknown;

    public async Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken)
            .ConfigureAwait(false);
        CapabilityWorkflowRoute? route = routes.FirstOrDefault(
            item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase));

        return new WorkflowSelection(
            route?.WorkflowName ?? "ProductReturnWorkflow",
            Type,
            route?.Reason ?? "Product return capability selected ProductReturnWorkflow.",
            CapabilityKey: Key);
    }
}
