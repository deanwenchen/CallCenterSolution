using System.Text.RegularExpressions;

namespace CallCenter.Framework.Safety;

/// <summary>
/// PII（个人身份信息）脱敏。使用正则匹配并替换为星号。
/// 支持：中国手机号（138****1234）、身份证号（110101********1234）、银行卡号（6222****1234）。
/// 应用于用户输入和 AI 输出，防止敏感信息泄露。
/// </summary>
public static partial class PiiRedactor
{
    // Chinese phone: 138****1234 (preserve 3 prefix + 4 suffix)
    [GeneratedRegex(@"(1[3-9]\d)\d{4}(\d{4})")]
    public static partial Regex PhonePattern();

    // Chinese ID card: 110101********1234 (preserve 6 prefix + 4 suffix)
    [GeneratedRegex(@"(\d{6})\d{8}(\d{4})")]
    public static partial Regex IdCardPattern();

    // Bank card: 6222****1234 (preserve 4 prefix + 4 suffix)
    [GeneratedRegex(@"(\d{4})\d{8}(\d{4})")]
    public static partial Regex BankCardPattern();

    /// <summary>Applies all PII redaction patterns to the input string.</summary>
    public static string Redact(string input)
    {
        input = PhonePattern().Replace(input, "$1****$2");
        input = IdCardPattern().Replace(input, "$1********$2");
        input = BankCardPattern().Replace(input, "$1********$2");
        return input;
    }
}
