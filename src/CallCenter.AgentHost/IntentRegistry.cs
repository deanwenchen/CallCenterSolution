using System.Text.Json;

namespace CallCenter.AgentHost;

/// <summary>意图定义 — 描述、参数模板。</summary>
public sealed record IntentDef(string Description, Dictionary<string, string>? Parameters = null);

/// <summary>
/// 意图注册表。
/// 主要作用：集中定义所有意图及其参数模板，System Prompt 从此处自动生成。
/// 以后加意图只改这一个字典。
/// 路由逻辑（if/switch）保留在 EntryPoint，直观好读。
/// </summary>
public static class IntentRegistry
{
    public static IReadOnlyDictionary<string, IntentDef> All { get; } = new Dictionary<string, IntentDef>
    {
        ["refund"] = new IntentDef(
            Description: "处理用户退款、退货、取消订单请求",
            Parameters: new Dictionary<string, string>
            {
                ["OrderId"] = "订单号（如果用户提到）",
            }),
        ["exchange"] = new IntentDef(
            Description: "处理用户换货、更换商品请求",
            Parameters: new Dictionary<string, string>
            {
                ["OrderId"] = "订单号（如果用户提到）",
            }),
        ["greeting"] = new IntentDef(Description: "问候语，如你好、再见等"),
        ["unknown"]  = new IntentDef(Description: "无法识别或不属于已知分类的请求"),
    };

    /// <summary>自动生成 System Prompt。</summary>
    public static string BuildSystemPrompt()
    {
        var intentJson = JsonSerializer.Serialize(All, new JsonSerializerOptions { WriteIndented = true });

        return $@"你是一个意图识别助手。根据以下意图字典，识别用户文本的意图：

{intentJson}

请严格按照以下 JSON 格式返回结果，不要返回其他任何内容：
{{
  ""intent"": ""意图标识"",
  ""parameters"": {{ 需要提取的参数，按上面定义的 key 填入 }}
}}

如果该意图不需要参数，parameters 返回空对象 {{}}。
注意：如果用户输入无法匹配上述任何意图，必须返回 intent 为 ""unknown"" 的结果。
只返回JSON，不要有任何其他文字。";
    }

    /// <summary>有参数的意图才是业务意图（greeting/unknown 没有 Parameters）。</summary>
    public static bool IsBusinessIntent(string intent) =>
        All.TryGetValue(intent, out var def) && def.Parameters is { Count: > 0 };
}
