using System;

namespace CallCenter.Framework.Safety;

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
