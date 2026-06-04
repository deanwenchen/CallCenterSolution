using System;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 安全违规异常。当用户输入触发关键词拦截或提示注入检测时抛出。
/// 携带违规类型供下游处理和日志记录。
/// </summary>
public class SafetyViolationException : Exception
{
    public string ViolationType { get; }

    public SafetyViolationException(string violationType, string message) : base(message)
    {
        ViolationType = violationType;
    }
}

/// <summary>
/// 多阶段输入安全过滤器。
/// 主要作用：在用户消息进入 LLM 之前做预处理，拦截危险输入、减少敏感信息泄露风险。
/// 管道顺序：PII 脱敏 → 关键词拦截 → 提示注入检测。
/// 任一阶段可抛出 SafetyViolationException，短路整个管道。
/// </summary>
public static class SafetyInputFilter
{
    public static string ProcessInput(string input, string sessionId)
    {
        return ProcessInput(input, sessionId, keywordFilter: null);
    }

    /// <summary>
    /// 处理用户输入，应用安全过滤。
    /// 当 keywordFilter 为 null 时，回退到静态默认关键词（向后兼容）。
    /// </summary>
    public static string ProcessInput(string input, string sessionId, KeywordFilter? keywordFilter)
    {
        // Step 1: PII redaction — mask sensitive data before any further processing
        var redacted = PiiRedactor.Redact(input);

        // Step 2: Keyword blocking — reject messages containing escalation-trigger words
        if (keywordFilter != null)
        {
            if (keywordFilter.IsBlocked(redacted))
            {
                var keyword = keywordFilter.GetBlockedKeyword(redacted);
                throw new SafetyViolationException("keyword_blocked", $"Input blocked by keyword filter: {keyword}");
            }
        }
        else
        {
            // Fallback to static default keywords
            if (KeywordFilter.IsBlockedStatic(redacted))
            {
                var keyword = KeywordFilter.GetBlockedKeywordStatic(redacted);
                throw new SafetyViolationException("keyword_blocked", $"Input blocked by keyword filter: {keyword}");
            }
        }

        // Step 3: Prompt injection detection — catch attempts to override system instructions
        if (PromptInjectionDetector.Detect(redacted))
        {
            throw new SafetyViolationException("injection_detected", "Potential prompt injection detected in input");
        }

        return redacted;
    }
}

/// <summary>
/// 输出安全过滤器。在 AI 响应到达用户之前应用。
/// 默认仅做 PII 脱敏；当配置 OutputContentFilter 后，额外执行输出端敏感内容拦截。
/// </summary>
public static class SafetyOutputFilter
{
    /// <summary>仅做 PII 脱敏（向后兼容）。</summary>
    public static string ProcessOutput(string output)
    {
        return PiiRedactor.Redact(output);
    }

    /// <summary>
    /// 处理 AI 输出，应用 PII 脱敏和可选的输出端内容审核。
    /// 当 contentFilter 不为 null 且 options.BlockedOutputCategories 非空时，
    /// 对脱敏后的内容执行关键词匹配拦截，命中则抛出 SafetyViolationException。
    /// </summary>
    public static string ProcessOutput(string output, SafetyOptions? options, OutputContentFilter? contentFilter)
    {
        var redacted = PiiRedactor.Redact(output);

        if (contentFilter != null && options != null && options.BlockedOutputCategories is { Length: > 0 })
        {
            if (contentFilter.IsBlocked(redacted))
            {
                var category = contentFilter.GetMatchedCategory(redacted);
                var message = category switch
                {
                    "violence" => options.ViolenceMessageTemplate,
                    "pornography" => options.PornographyMessageTemplate,
                    "politics" => options.PoliticsMessageTemplate,
                    _ => options.BlockedMessageTemplate,
                };
                throw new SafetyViolationException("output_content_blocked", message);
            }
        }

        return redacted;
    }
}
