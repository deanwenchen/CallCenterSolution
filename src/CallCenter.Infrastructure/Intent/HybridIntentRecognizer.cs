using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// 规则优先、模型兜底的意图识别器。
/// </summary>
public sealed class HybridIntentRecognizer : IIntentRecognizer
{
    private const double ModelFallbackThreshold = 0.6;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly KeywordIntentRecognizer _keywordIntentRecognizer;
    private readonly IModelClient _modelClient;
    private readonly IEnumerable<IIntentDefinitionProvider> _intentDefinitionProviders;

    public async Task<IntentResult> RecognizeAsync(
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default)
    {
        IntentResult ruleResult = await _keywordIntentRecognizer
            .RecognizeAsync(session, message, cancellationToken)
            .ConfigureAwait(false);

        if (ruleResult.Confidence >= ModelFallbackThreshold)
        {
            return ruleResult;
        }

        IntentDefinition[] definitions = await _definitionsLazy.Value.ConfigureAwait(false);
        if (definitions.Length == 0)
        {
            return ruleResult;
        }

        ModelChatResponse modelResponse = await _modelClient
            .CompleteAsync(BuildRequest(session, message, ruleResult, definitions), cancellationToken)
            .ConfigureAwait(false);

        if (!modelResponse.Succeeded || string.IsNullOrWhiteSpace(modelResponse.Content))
        {
            return ruleResult;
        }

        if (TryConvertModelResult(modelResponse.Content, ruleResult, definitions, out IntentResult? modelResult) &&
            modelResult is not null)
        {
            return modelResult;
        }

        return ruleResult;
    }

    private static ModelChatRequest BuildRequest(
        SessionContext session,
        string message,
        IntentResult ruleResult,
        IReadOnlyCollection<IntentDefinition> definitions)
    {
        string intentCatalog = BuildIntentCatalog(definitions);
        string knownEntities = JsonSerializer.Serialize(ruleResult.Entities, JsonOptions);

        string systemPrompt =
            """
            You are an intent classifier for a call center workflow system.
            Return only one JSON object. Do not include markdown or explanation.
            You must choose intentKey only from the provided catalog.
            If the user asks for a human agent, choose HumanAgent.
            If the intent is unclear, choose Unknown with confidence below 0.5.
            Extract business entities from the user message when present.
            JSON schema:
            {
              "intentKey": "one catalog key or Unknown",
              "intent": "one IntentType enum name",
              "confidence": 0.0,
              "entities": {
                "orderId": "optional",
                "amount": "optional",
                "invoiceTitle": "optional"
              }
            }
            """;

        string userPrompt =
            $"""
            Tenant: {session.TenantId}
            Channel: {session.Channel}
            Intent catalog:
            {intentCatalog}

            Rule result:
            intent={ruleResult.Intent}
            intentKey={ruleResult.Key}
            confidence={ruleResult.Confidence}
            entities={knownEntities}

            User message:
            {message}
            """;

        return new ModelChatRequest(
        [
            new ModelChatMessage(ModelMessageRole.System, systemPrompt),
            new ModelChatMessage(ModelMessageRole.User, userPrompt)
        ],
        ResponseFormat: "json_object");
    }

    private static string BuildIntentCatalog(IEnumerable<IntentDefinition> definitions)
    {
        var builder = new StringBuilder();
        foreach (IntentDefinition definition in definitions.OrderBy(definition => definition.Key))
        {
            string keywords = string.Join(", ", definition.Keywords);
            builder.Append("- ")
                .Append(definition.Key)
                .Append(" => ")
                .Append(definition.Intent)
                .Append("; keywords: ")
                .AppendLine(keywords);
        }

        builder.AppendLine("- Unknown => Unknown; use when no catalog intent fits.");
        return builder.ToString();
    }

    private static bool TryConvertModelResult(
        string content,
        IntentResult ruleResult,
        IReadOnlyCollection<IntentDefinition> definitions,
        out IntentResult? result)
    {
        result = null;

        // Strip markdown code fences if present (e.g., ```json ... ```).
        string json = content.Trim();
        int start = json.IndexOf("```", StringComparison.Ordinal);
        if (start >= 0)
        {
            // Skip past "```json" or "```" marker.
            int innerStart = json.IndexOf('\n', start) + 1;
            int end = json.IndexOf("```", innerStart);
            if (end > innerStart)
            {
                json = json.Substring(innerStart, end - innerStart).Trim();
            }
        }

        try
        {
            ModelIntentResult? modelResult = JsonSerializer.Deserialize<ModelIntentResult>(json, JsonOptions);
            if (modelResult is null || modelResult.Confidence < 0 || modelResult.Confidence > 1)
            {
                return false;
            }

            Dictionary<string, IntentDefinition> byKey = definitions.ToDictionary(
                definition => definition.Key,
                StringComparer.OrdinalIgnoreCase);

            string intentKey = string.IsNullOrWhiteSpace(modelResult.IntentKey)
                ? modelResult.Intent.ToString()
                : modelResult.IntentKey;

            IntentType intent;
            string? normalizedIntentKey;
            if (byKey.TryGetValue(intentKey, out IntentDefinition? definition))
            {
                intent = definition.Intent;
                normalizedIntentKey = definition.Key;
            }
            else if (modelResult.Intent == IntentType.Unknown || intentKey.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                intent = IntentType.Unknown;
                normalizedIntentKey = null;
            }
            else
            {
                return false;
            }

            Dictionary<string, string> entities = new(ruleResult.Entities, StringComparer.OrdinalIgnoreCase);
            if (modelResult.Entities is not null)
            {
                foreach ((string key, string value) in modelResult.Entities)
                {
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                    {
                        entities[key] = value;
                    }
                }
            }

            result = new IntentResult(intent, modelResult.Confidence, entities, normalizedIntentKey);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private readonly Lazy<Task<IntentDefinition[]>> _definitionsLazy;

    public HybridIntentRecognizer(
        KeywordIntentRecognizer keywordIntentRecognizer,
        IModelClient modelClient,
        IEnumerable<IIntentDefinitionProvider> intentDefinitionProviders)
    {
        _keywordIntentRecognizer = keywordIntentRecognizer;
        _modelClient = modelClient;
        _intentDefinitionProviders = intentDefinitionProviders;
        _definitionsLazy = new Lazy<Task<IntentDefinition[]>>(LoadIntentDefinitionsAsync);
    }

    private async Task<IntentDefinition[]> LoadIntentDefinitionsAsync()
    {
        var definitions = new List<IntentDefinition>();
        foreach (IIntentDefinitionProvider provider in _intentDefinitionProviders)
        {
            definitions.AddRange(await provider.GetIntentDefinitionsAsync(CancellationToken.None).ConfigureAwait(false));
        }

        return definitions
            .GroupBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
    }

    private sealed record ModelIntentResult(
        string IntentKey,
        IntentType Intent,
        double Confidence,
        Dictionary<string, string>? Entities);
}
