using System.Text.RegularExpressions;
using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

public sealed partial class RuleBasedIntentRecognizer : IIntentRecognizer
{
    // 意图层只做分类与实体抽取，不执行任何业务动作。
    public Task<IntentResult> RecognizeAsync(
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> entities = ExtractEntities(message);
        string normalized = message.ToLowerInvariant();

        IntentType intent = normalized switch
        {
            var text when text.Contains("refund", StringComparison.Ordinal) ||
                          text.Contains("退款", StringComparison.Ordinal) ||
                          text.Contains("退货", StringComparison.Ordinal) => IntentType.Refund,
            var text when text.Contains("logistics", StringComparison.Ordinal) ||
                          text.Contains("tracking", StringComparison.Ordinal) ||
                          text.Contains("物流", StringComparison.Ordinal) ||
                          text.Contains("快递", StringComparison.Ordinal) => IntentType.Logistics,
            var text when text.Contains("invoice", StringComparison.Ordinal) ||
                          text.Contains("发票", StringComparison.Ordinal) => IntentType.Invoice,
            var text when text.Contains("crm", StringComparison.Ordinal) ||
                          text.Contains("标签", StringComparison.Ordinal) => IntentType.Crm,
            var text when text.Contains("subscribe", StringComparison.Ordinal) ||
                          text.Contains("订阅", StringComparison.Ordinal) => IntentType.Subscribe,
            _ => IntentType.Unknown
        };

        return Task.FromResult(new IntentResult(intent, intent == IntentType.Unknown ? 0.2 : 0.9, entities));
    }

    private static Dictionary<string, string> ExtractEntities(string message)
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

        return entities;
    }

    [GeneratedRegex(@"[A-Z]\d{3,}|\bORD[-_]?\d+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderIdRegex();

    [GeneratedRegex(@"(?:amount|金额|￥|\$)\s*[:：]?\s*(?<amount>\d+(\.\d{1,2})?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountRegex();
}

public sealed class DefaultPlanner : IPlanner
{
    // Planner 只选择 Capability；风险判断和 Workflow 选择交给 Capability/Policy 层。
    public Task<CapabilitySelection> SelectCapabilityAsync(
        IntentResult intent,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        CapabilityType capability = intent.Intent switch
        {
            IntentType.Refund => CapabilityType.Refund,
            IntentType.Logistics => CapabilityType.Logistics,
            IntentType.Invoice => CapabilityType.Invoice,
            IntentType.Crm => CapabilityType.Crm,
            IntentType.Subscribe => CapabilityType.Subscribe,
            _ => CapabilityType.HumanAgent
        };

        return Task.FromResult(new CapabilitySelection(capability, $"Intent {intent.Intent} mapped to capability {capability}."));
    }
}
