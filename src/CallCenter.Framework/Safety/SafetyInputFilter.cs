using System;

namespace CallCenter.Framework.Safety;

/// <summary>
/// Thrown when user input violates safety policies (keyword blocking, prompt injection).
/// Carries the violation type for downstream handling and logging.
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
/// Multi-stage input safety filter applied before messages reach the LLM.
/// Pipeline order: PII redaction → keyword blocking → prompt injection detection.
/// Any stage can throw SafetyViolationException, which short-circuits the pipeline.
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
/// Output safety filter applied to AI responses before they reach the user.
/// Currently only PII redaction; extend as needed for content moderation.
/// </summary>
public static class SafetyOutputFilter
{
    public static string ProcessOutput(string output)
    {
        return PiiRedactor.Redact(output);
    }
}
