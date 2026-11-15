using System.Text.RegularExpressions;

namespace Nawly.Domain.ValueObjects;

/// <summary>
/// Egyptian mobile phone value object normalized to E.164 (+2010/11/12/15 + 8 digits, SRS §8.3 SEC-AUTH-07).
/// </summary>
public sealed partial record PhoneNumber
{
    public string Value { get; }

    private PhoneNumber(string normalizedE164)
    {
        Value = normalizedE164;
    }

    public static bool TryParseEgyptianMobile(string? input, out PhoneNumber? phoneNumber)
    {
        phoneNumber = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var cleaned = CleanDigits(input);

        if (cleaned.StartsWith("0020", StringComparison.Ordinal))
            cleaned = "+" + cleaned[2..];
        else if (cleaned.StartsWith("20", StringComparison.Ordinal) && cleaned.Length == 12)
            cleaned = "+" + cleaned;
        else if (cleaned.StartsWith("01", StringComparison.Ordinal) && cleaned.Length == 11)
            cleaned = "+2" + cleaned;
        else if (!cleaned.StartsWith("+20", StringComparison.Ordinal) && cleaned.StartsWith('1') && cleaned.Length == 10)
            cleaned = "+20" + cleaned;

        if (!EgyptianMobileRegex().IsMatch(cleaned))
            return false;

        phoneNumber = new PhoneNumber(cleaned);
        return true;
    }

    public static PhoneNumber ParseEgyptianMobile(string input)
    {
        if (!TryParseEgyptianMobile(input, out var phone) || phone is null)
            throw new ArgumentException($"Invalid Egyptian mobile number: '{MaskRaw(input)}'.", nameof(input));

        return phone;
    }

    /// <summary>
    /// Masks middle digits for logs and admin summaries (SRS §8.8 SEC-DATA-07, §8.11).
    /// </summary>
    public string Mask() => MaskRaw(Value);

    public static string MaskRaw(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.Trim();
        if (trimmed.Length <= 6) return new string('*', trimmed.Length);
        return string.Concat(trimmed.AsSpan(0, 5), new string('*', trimmed.Length - 8), trimmed.AsSpan(trimmed.Length - 3));
    }

    private static string CleanDigits(string input)
    {
        var chars = input.Where(c => char.IsAsciiDigit(c) || c == '+').ToArray();
        return new string(chars);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^\+20(10|11|12|15)\d{8}$", RegexOptions.Compiled)]
    private static partial Regex EgyptianMobileRegex();
}
