using System;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 提示注入检测器。匹配已知攻击模式：
/// - 指令覆盖："忽略之前"、"ignore previous"、"override instructions"
/// - 系统提示提取："system prompt"、"system指令"
/// - 角色扮演："you are now"、"你现在是"
/// - 绕过安全："绕过安全"
/// 不区分大小写匹配。作为 SafetyInputFilter 管道的一部分使用。
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
