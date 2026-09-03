using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Bricker.Api.Validation;

public static partial class InputValidation
{
    public static string Digits(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : NonDigitRegex().Replace(value, string.Empty);

    public static bool IsValidWhatsApp(string? value)
    {
        var digits = Digits(value);
        return digits.Length is 10 or 11 && digits[0] != '0' && digits.Distinct().Count() > 1;
    }

    public static bool IsValidPostalCode(string? value) => Digits(value).Length == 8;

    public static bool IsValidState(string? value) =>
        !string.IsNullOrWhiteSpace(value) && StateRegex().IsMatch(value.Trim());

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 254 && new EmailAddressAttribute().IsValid(value);

    [GeneratedRegex("\\D")]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex("^[A-Za-z]{2}$")]
    private static partial Regex StateRegex();
}
