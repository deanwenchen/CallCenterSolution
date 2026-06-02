using System.Collections.Generic;

namespace CallCenter.Framework.Safety;

/// <summary>
/// Keyword-based content filter for blocking escalation-trigger words.
/// Covers complaint/legal terms (投诉, 举报, 起诉), violence (暴力, 杀人), fraud (诈骗), etc.
/// Case-insensitive matching. Used as part of the SafetyInputFilter pipeline.
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
