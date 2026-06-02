using System.Text.Json;

namespace CallCenter.Framework.Parsing;

/// <summary>
/// LLM 结构化输出解析器。
/// 1. 剥离 Markdown 代码围栏（```json ... ```），这是 LLM 常用的 JSON 包装格式
/// 2. 将 JSON 反序列化为指定类型 TOutput
/// 3. 解析失败时返回 null（不抛异常）
/// 用于解析 LLM 意图识别返回的 JSON 结果。
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
