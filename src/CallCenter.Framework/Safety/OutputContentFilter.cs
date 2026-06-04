using System;
using System.Collections.Generic;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 输出端内容过滤器。按类别（violence/pornography/politics）对 LLM 生成内容进行关键词匹配拦截。
/// 采用与 KeywordFilter 一致的实例模式，通过 SafetyOptions 配置关键词，不硬编码。
/// 作为 SafetyOutputFilter 管道的一部分使用。
/// </summary>
public class OutputContentFilter
{
    private readonly string[] _violenceKeywords;
    private readonly string[] _pornographyKeywords;
    private readonly string[] _politicsKeywords;

    /// <summary>使用指定的安全选项创建输出内容过滤器。</summary>
    public OutputContentFilter(SafetyOptions options)
    {
        _violenceKeywords = options.ViolenceKeywords ?? Array.Empty<string>();
        _pornographyKeywords = options.PornographyKeywords ?? Array.Empty<string>();
        _politicsKeywords = options.PoliticsKeywords ?? Array.Empty<string>();
    }

    /// <summary>如果输出包含任何启用类别的关键词，返回 true。</summary>
    public bool IsBlocked(string output)
    {
        return GetMatchedCategory(output) is not null;
    }

    /// <summary>返回第一个匹配到的类别标识符（"violence" / "pornography" / "politics"），无匹配返回 null。</summary>
    public string? GetMatchedCategory(string output)
    {
        if (ContainsAny(output, _violenceKeywords))
            return "violence";
        if (ContainsAny(output, _pornographyKeywords))
            return "pornography";
        if (ContainsAny(output, _politicsKeywords))
            return "politics";
        return null;
    }

    /// <summary>返回输出中找到的第一个被拦截关键词，无匹配返回 null。</summary>
    public string? GetFirstMatchedKeyword(string output)
    {
        var keyword = FindFirst(output, _violenceKeywords);
        if (keyword is not null) return keyword;
        keyword = FindFirst(output, _pornographyKeywords);
        if (keyword is not null) return keyword;
        return FindFirst(output, _politicsKeywords);
    }

    private static bool ContainsAny(string output, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (output.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string? FindFirst(string output, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (output.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return keyword;
        }
        return null;
    }
}
