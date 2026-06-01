using System;

namespace CallCenter.Framework.Safety;

public class SafetyViolationException : Exception
{
    public string ViolationType { get; }

    public SafetyViolationException(string violationType, string message) : base(message)
    {
        ViolationType = violationType;
    }
}

public static class SafetyInputFilter
{
    public static string ProcessInput(string input, string sessionId)
    {
        // Step 1: PII redaction
        var redacted = PiiRedactor.Redact(input);

        // Step 2: Keyword blocking
        if (KeywordFilter.IsBlocked(redacted))
        {
            var keyword = KeywordFilter.GetBlockedKeyword(redacted);
            throw new SafetyViolationException("keyword_blocked", $"Input blocked by keyword filter: {keyword}");
        }

        // Step 3: Prompt injection detection
        if (PromptInjectionDetector.Detect(redacted))
        {
            throw new SafetyViolationException("injection_detected", "Potential prompt injection detected in input");
        }

        return redacted;
    }
}

public static class SafetyOutputFilter
{
    public static string ProcessOutput(string output)
    {
        return PiiRedactor.Redact(output);
    }
}
