using System.Collections.Generic;

namespace CallCenter.Framework.Safety;

public static class KeywordFilter
{
    private static readonly HashSet<string> BlockedKeywords = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "投诉", "举报", "起诉", "维权",
        "暴力", "杀人", "自杀", "死",
        "诈骗", "欺诈", "骗子",
        "非法", "违规",
    };

    public static bool IsBlocked(string input)
    {
        foreach (var keyword in BlockedKeywords)
        {
            if (input.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

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
