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

    // ===== Output-end content filtering =====

    /// <summary>启用的输出端审核类别。默认包含 violence、pornography、politics 三类。</summary>
    public string[] BlockedOutputCategories { get; set; } = new[] { "violence", "pornography", "politics" };

    /// <summary>暴力类拦截话术模板。</summary>
    public string ViolenceMessageTemplate { get; set; } = "抱歉，系统无法提供相关内容。如需帮助，请联系人工客服。";

    /// <summary>色情类拦截话术模板。</summary>
    public string PornographyMessageTemplate { get; set; } = "抱歉，系统无法提供相关内容。如需帮助，请联系人工客服。";

    /// <summary>政治类拦截话术模板。</summary>
    public string PoliticsMessageTemplate { get; set; } = "抱歉，该问题超出我的服务范围。如有其他问题，我很乐意帮助您。";

    /// <summary>暴力类关键词列表。用于 OutputContentFilter 匹配。</summary>
    public string[] ViolenceKeywords { get; set; } = new[]
    {
        "血腥", "屠杀", "砍杀", "肢解", "炸死", "恐怖袭击", "自杀式", "人体炸弹",
    };

    /// <summary>色情类关键词列表。用于 OutputContentFilter 匹配。</summary>
    public string[] PornographyKeywords { get; set; } = new[]
    {
        "黄色", "色情", "淫秽", "裸露", "性交", "裸体", "激情", "av片",
    };

    /// <summary>政治类关键词列表。用于 OutputContentFilter 匹配。</summary>
    public string[] PoliticsKeywords { get; set; } = new[]
    {
        "台独", "藏独", "疆独", "港独", "法轮功", "民运", "六四", "天安门事件",
    };
}
