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
        // Step 1: PII redaction — mask sensitive data before any further processing
        var redacted = PiiRedactor.Redact(input);

        // Step 2: Keyword blocking — reject messages containing escalation-trigger words
        if (KeywordFilter.IsBlocked(redacted))
        {
            var keyword = KeywordFilter.GetBlockedKeyword(redacted);
            throw new SafetyViolationException("keyword_blocked", $"Input blocked by keyword filter: {keyword}");
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
/// 目前仅做 PII 脱敏；需要时可扩展内容审核功能。
/// </summary>
public static class SafetyOutputFilter
{
    public static string ProcessOutput(string output)
    {
        return PiiRedactor.Redact(output);
    }
}
