using System.Text.RegularExpressions;

namespace CallCenter.Framework.Safety;

public static partial class PiiRedactor
{
    [GeneratedRegex(@"(1[3-9]\d)\d{4}(\d{4})")]
    public static partial Regex PhonePattern();

    [GeneratedRegex(@"(\d{6})\d{8}(\d{4})")]
    public static partial Regex IdCardPattern();

    [GeneratedRegex(@"(\d{4})\d{8}(\d{4})")]
    public static partial Regex BankCardPattern();

    public static string Redact(string input)
    {
        input = PhonePattern().Replace(input, "$1****$2");
        input = IdCardPattern().Replace(input, "$1********$2");
        input = BankCardPattern().Replace(input, "$1********$2");
        return input;
    }
}
