using System.Collections.Generic;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 关键词内容过滤器。用于拦截升级触发词。
/// 覆盖：投诉/法律类（投诉、举报、起诉、维权）、暴力类（暴力、杀人、自杀、死）、诈骗类（诈骗、欺诈、骗子）、违规类（非法、违规）。
/// 不区分大小写匹配。作为 SafetyInputFilter 管道的一部分使用。
/// </summary>
public static class KeywordFilter
{
    private static readonly HashSet<string> BlockedKeywords = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "投诉", "举报", "起诉", "维权",
        "暴力", "杀人", "自杀", "死",
        "诈骗", "欺诈", "骗子",
        "非法", "违规",
    };

    /// <summary>Returns true if any blocked keyword is found in the input.</summary>
    public static bool IsBlocked(string input)
    {
        foreach (var keyword in BlockedKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>Returns the first blocked keyword found, or null if none.</summary>
    public static string? GetBlockedKeyword(string input)
    {
        foreach (var keyword in BlockedKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return keyword;
        }
        return null;
    }
}
