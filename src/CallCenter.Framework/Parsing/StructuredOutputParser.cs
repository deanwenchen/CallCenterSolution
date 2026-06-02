using System.Text.Json;

namespace CallCenter.Framework.Parsing;

/// <summary>
/// Parses structured JSON output from LLM responses.
/// Strips markdown code fences (```json ... ```) that LLMs often wrap JSON in,
/// then deserializes into the target type.
/// </summary>
public class StructuredOutputParser<TOutput> where TOutput : class
{
    private readonly JsonSerializerOptions? _options;

    public StructuredOutputParser(JsonSerializerOptions? options = null)
    {
        _options = options;
    }

    /// <summary>
    /// Parses a JSON string (optionally wrapped in markdown code fences) into TOutput.
    /// Returns null if parsing fails.
    /// </summary>
    public TOutput? Parse(string json)
    {
        // Strip markdown code fences if present
        if (json.StartsWith("```"))
        {
            var lines = json.Split('\n');
            json = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }

        return JsonSerializer.Deserialize<TOutput>(json, _options);
    }
}
