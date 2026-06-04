using System.Collections.Generic;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 关键词内容过滤器。用于拦截升级触发词。
/// 覆盖：投诉/法律类（投诉、举报、起诉、维权）、暴力类（暴力、杀人、自杀、死）、诈骗类（诈骗、欺诈、骗子）、违规类（非法、违规）。
/// 不区分大小写匹配。作为 SafetyInputFilter 管道的一部分使用。
/// 支持实例模式（通过 SafetyOptions 配置关键词）和静态模式（使用默认关键词，向后兼容）。
/// </summary>
public class KeywordFilter
{
    private readonly HashSet<string> _blockedKeywords;

    /// <summary>使用默认关键词创建过滤器（向后兼容）。</summary>
    public KeywordFilter()
        : this(new SafetyOptions())
    {
    }

    /// <summary>使用指定的安全选项创建过滤器。</summary>
    public KeywordFilter(SafetyOptions options)
    {
        _blockedKeywords = new HashSet<string>(options.BlockedKeywords, System.StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>如果输入包含任何被拦截的关键词，返回 true。</summary>
    public bool IsBlocked(string input)
    {
        foreach (var keyword in _blockedKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>返回输入中找到的第一个被拦截关键词，如果没有则返回 null。</summary>
    public string? GetBlockedKeyword(string input)
    {
        foreach (var keyword in _blockedKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return keyword;
        }
        return null;
    }

    // ===== Static API (backwards compatibility) =====

    private static readonly HashSet<string> DefaultKeywords = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "投诉", "举报", "起诉", "维权",
        "暴力", "杀人", "自杀", "死",
        "诈骗", "欺诈", "骗子",
        "非法", "违规",
    };

    /// <summary>静态方法：使用默认关键词检查输入是否被拦截（向后兼容）。</summary>
    public static bool IsBlockedStatic(string input)
    {
        foreach (var keyword in DefaultKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>静态方法：返回输入中找到的第一个被拦截关键词（向后兼容）。</summary>
    public static string? GetBlockedKeywordStatic(string input)
    {
        foreach (var keyword in DefaultKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return keyword;
        }
        return null;
    }
}
