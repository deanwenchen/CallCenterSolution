using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// Maps recognized intents to business capabilities using configured routes.
/// </summary>
public sealed class IntentCapabilityPlanner(IEnumerable<IIntentCapabilityRouteProvider> routeProviders) : IPlanner
{
    public async Task<CapabilitySelection> SelectCapabilityAsync(
        IntentResult intent,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        if (intent.Confidence < 0.5)
        {
            return new CapabilitySelection(CapabilityType.HumanAgent, "Low confidence intent routed to human agent.");
        }

        IntentCapabilityRoute[] routes = await LoadRoutesAsync(cancellationToken).ConfigureAwait(false);

        IntentCapabilityRoute? route = routes.FirstOrDefault(
            item => string.Equals(item.IntentKey, intent.Key, StringComparison.OrdinalIgnoreCase));

        if (route is null)
        {
            return new CapabilitySelection(CapabilityType.HumanAgent, $"No capability route configured for intent {intent.Key}.");
        }

        return new CapabilitySelection(route.Capability, route.Reason, route.CapabilityKey);
    }

    private async Task<IntentCapabilityRoute[]> LoadRoutesAsync(CancellationToken cancellationToken)
    {
        var routes = new List<IntentCapabilityRoute>();
        foreach (IIntentCapabilityRouteProvider provider in routeProviders)
        {
            routes.AddRange(await provider.GetRoutesAsync(cancellationToken).ConfigureAwait(false));
        }

        return routes
            .GroupBy(route => route.IntentKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
    }
}
