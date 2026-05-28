using System.Text.RegularExpressions;
using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 配置驱动的意图识别器。
/// </summary>
public sealed partial class ConfiguredIntentRecognizer(IIntentDefinitionProvider intentDefinitionProvider) : IIntentRecognizer
{
    public async Task<IntentResult> RecognizeAsync(
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> entities = ExtractEntities(message);
        string normalized = message.ToLowerInvariant();
        IReadOnlyCollection<IntentDefinition> definitions = await intentDefinitionProvider
            .GetIntentDefinitionsAsync(cancellationToken)
            .ConfigureAwait(false);

        IntentDefinition? matched = definitions
            .Where(definition => definition.Keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(definition => definition.Confidence)
            .FirstOrDefault();

        if (matched is null)
        {
            IntentType fallbackIntent = message.Length > 120 ? IntentType.HumanAgent : IntentType.Unknown;
            double fallbackConfidence = fallbackIntent == IntentType.HumanAgent ? 0.55 : 0.2;
            return new IntentResult(fallbackIntent, fallbackConfidence, entities);
        }

        return new IntentResult(matched.Intent, matched.Confidence, entities, matched.Key);
    }

    internal static Dictionary<string, string> ExtractEntities(string message)
    {
        var entities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Match orderMatch = OrderIdRegex().Match(message);
        if (orderMatch.Success)
        {
            entities["orderId"] = orderMatch.Value;
        }

        Match amountMatch = AmountRegex().Match(message);
        if (amountMatch.Success)
        {
            entities["amount"] = amountMatch.Groups["amount"].Value;
        }

        Match invoiceMatch = InvoiceTitleRegex().Match(message);
        if (invoiceMatch.Success)
        {
            entities["invoiceTitle"] = invoiceMatch.Groups["title"].Value.Trim();
        }

        return entities;
    }

    [GeneratedRegex(@"[A-Z]\d{3,}|\bORD[-_]?\d+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderIdRegex();

    [GeneratedRegex(@"(?:amount|金额|￥|\$)\s*[:：]?\s*(?<amount>\d+(\.\d{1,2})?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"(?:invoice title|发票抬头)\s*[:：]?\s*(?<title>[\w\u4e00-\u9fa5\- ]{2,40})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceTitleRegex();
}

/// <summary>
/// 配置驱动的 Planner。
/// </summary>
public sealed class ConfiguredPlanner(IIntentCapabilityRouteProvider routeProvider) : IPlanner
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

        IReadOnlyCollection<IntentCapabilityRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken)
            .ConfigureAwait(false);

        IntentCapabilityRoute? route = routes.FirstOrDefault(
            item => string.Equals(item.IntentKey, intent.Key, StringComparison.OrdinalIgnoreCase));

        if (route is null)
        {
            return new CapabilitySelection(CapabilityType.HumanAgent, $"No capability route configured for intent {intent.Key}.");
        }

        return new CapabilitySelection(route.Capability, route.Reason, route.CapabilityKey);
    }
}
