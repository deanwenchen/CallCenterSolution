using System;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 提示注入检测器。匹配已知攻击模式：
/// - 指令覆盖："忽略之前"、"ignore previous"、"ignore all"、"disregard all"
/// - 系统提示提取："system prompt"、"system指令"、"your instructions are"
/// - 角色扮演："you are now"、"你现在是"、"act as"、"pretend to be"
/// - 绕过安全："绕过安全"
/// - DAN/越狱："DAN mode"、"jailbreak"、"developer mode"
/// - 代码注入："```"、"code block"
/// 不区分大小写匹配。作为 SafetyInputFilter 管道的一部分使用。
/// </summary>
public static class PromptInjectionDetector
{
    private static readonly string[] InjectionPatterns = new[]
    {
        // 指令覆盖
        "忽略之前", "ignore previous", "ignore above",
        "ignore all", "disregard all", "forget all",
        "do not follow", "override instructions",
        // 系统提示提取
        "system prompt", "system指令",
        "your instructions are", "what is your system prompt",
        "reveal your prompt", "show me your system prompt",
        // 角色扮演
        "you are now", "你现在是",
        "扮演系统", "act as", "pretend to be",
        "从此刻起", "pretend you are",
        // 绕过安全
        "绕过安全",
        " disregard",
        // DAN/越狱
        "dan mode", "jailbreak",
        "developer mode", "unrestricted mode",
        // 代码注入
        "```",
        "code block",
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
