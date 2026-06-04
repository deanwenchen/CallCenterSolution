namespace CallCenter.Framework.Safety;

/// <summary>
/// 安全管道配置选项。
/// 从 appsettings.json 的 "Safety" 段读取，用于配置关键词过滤和注入检测。
/// </summary>
public class SafetyOptions
{
    /// <summary>
    /// 被拦截的关键词列表。默认包含当前硬编码的 14 个关键词。
    /// </summary>
    public string[] BlockedKeywords { get; set; } = new[]
    {
        "投诉", "举报", "起诉", "维权",
        "暴力", "杀人", "自杀", "死",
        "诈骗", "欺诈", "骗子",
        "非法", "违规",
    };

    /// <summary>
    /// 拦截消息模板。{keyword} 占位符会被实际命中的关键词替换。
    /// </summary>
    public string BlockedMessageTemplate { get; set; } = "您的输入包含敏感内容（{keyword}），我们已暂时中止处理。如有需要，请联系人工客服。";

    /// <summary>是否启用关键词过滤。默认 true。</summary>
    public bool EnableKeywordFilter { get; set; } = true;

    /// <summary>是否启用注入检测。默认 true。</summary>
    public bool EnableInjectionDetection { get; set; } = true;
}
