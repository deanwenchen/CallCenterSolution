using System.Text.RegularExpressions;
using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// 基于配置关键词和本地正则实体抽取的意图识别器。
/// </summary>
public sealed partial class KeywordIntentRecognizer(IEnumerable<IIntentDefinitionProvider> intentDefinitionProviders) : IIntentRecognizer
{
    public async Task<IntentResult> RecognizeAsync(
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> entities = ExtractEntities(message);
        IntentDefinition[] definitions = await LoadIntentDefinitionsAsync(cancellationToken).ConfigureAwait(false);

        IntentDefinition? matched = definitions
            .Where(definition => definition.Keywords.Any(keyword => MatchesKeyword(message, keyword)))
            .OrderByDescending(definition => definition.Confidence)
            .FirstOrDefault();

        if (matched is null)
        {
            IntentType fallbackIntent = message.Length > 120 ? IntentType.HumanAgent : IntentType.Unknown;
            double fallbackConfidence = fallbackIntent == IntentType.HumanAgent ? 0.4 : 0.2;
            return new IntentResult(fallbackIntent, fallbackConfidence, entities);
        }

        return new IntentResult(matched.Intent, matched.Confidence, entities, matched.Key);
    }

    private static bool MatchesKeyword(string message, string keyword)
    {
        // Chinese keywords use substring matching since CJK has no word boundaries.
        if (keyword.Any(c => c >= '一' && c <= '鿿'))
        {
            return message.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        // English keywords use word-boundary matching to avoid false positives (e.g., "tag" in "luggage").
        return Regex.IsMatch(message, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private async Task<IntentDefinition[]> LoadIntentDefinitionsAsync(CancellationToken cancellationToken)
    {
        var definitions = new List<IntentDefinition>();
        foreach (IIntentDefinitionProvider provider in intentDefinitionProviders)
        {
            definitions.AddRange(await provider.GetIntentDefinitionsAsync(cancellationToken).ConfigureAwait(false));
        }

        return definitions
            .GroupBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
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
