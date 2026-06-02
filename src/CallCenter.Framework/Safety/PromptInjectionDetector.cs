using System;

namespace CallCenter.Framework.Safety;

/// <summary>
/// Detects potential prompt injection attacks by matching known attack patterns.
/// Covers common techniques: instruction override ("忽略之前", "ignore previous"),
/// system prompt extraction ("system prompt"), role manipulation ("you are now"), etc.
/// Case-insensitive matching. Used as part of the SafetyInputFilter pipeline.
/// </summary>
public static class PromptInjectionDetector
{
    private static readonly string[] InjectionPatterns = new[]
    {
        "忽略之前", "ignore previous", "ignore above",
        "system prompt", "system指令",
        "you are now", "你现在是",
        "扮演系统", "绕过安全",
        " disregard", "override instructions",
    };

    /// <summary>Returns true if any injection pattern is detected in the input.</summary>
    public static bool Detect(string input)
    {
        var lower = input.ToLowerInvariant();
        foreach (var pattern in InjectionPatterns)
        {
            if (lower.Contains(pattern))
                return true;
        }
        return false;
    }
}
