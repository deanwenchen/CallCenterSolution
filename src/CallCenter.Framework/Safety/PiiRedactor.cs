using System.Text.RegularExpressions;

namespace CallCenter.Framework.Safety;

/// <summary>
/// PII (Personally Identifiable Information) redaction using regex patterns.
/// Masks Chinese phone numbers, ID cards, and bank card numbers by preserving
/// first/last digits and replacing the middle with asterisks.
/// Applied to both user input and AI output to prevent PII leakage.
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
