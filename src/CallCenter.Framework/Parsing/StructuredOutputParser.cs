using System.Text.Json;

namespace CallCenter.Framework.Parsing;

public class StructuredOutputParser<TOutput> where TOutput : class
{
    private readonly JsonSerializerOptions? _options;

    public StructuredOutputParser(JsonSerializerOptions? options = null)
    {
        _options = options;
    }

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
