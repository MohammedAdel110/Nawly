using System.Globalization;
using System.Text;

namespace Nawly.Domain.ValueObjects;

/// <summary>
/// Normalizes Arabic and English search text (SRS §2.8, §6.4):
/// removes tashkeel/diacritics and tatweel, normalizes alef/hamza, ya/alef-maqsura, and ta-marbuta.
/// </summary>
public static class ArabicNormalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var nfc = input.Trim().Normalize(NormalizationForm.FormC);
        var sb = new StringBuilder(nfc.Length);
        var prevSpace = false;

        foreach (var ch in nfc)
        {
            // Skip Arabic tatweel and diacritics / combining marks
            if (ch == '\u0640' || IsArabicDiacritic(ch))
                continue;

            var mapped = ch switch
            {
                '\u0622' or '\u0623' or '\u0625' or '\u0671' => '\u0627', // آ أ إ ٱ -> ا
                '\u0649' => '\u064A',                                      // ى -> ي
                '\u0629' => '\u0647',                                      // ة -> ه
                '\u0624' => '\u0648',                                      // ؤ -> و
                '\u0626' => '\u064A',                                      // ئ -> ي
                _ => char.ToLower(ch, CultureInfo.InvariantCulture)
            };

            if (char.IsWhiteSpace(mapped))
            {
                if (!prevSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(mapped);
                prevSpace = false;
            }
        }

        return sb.ToString().Trim();
    }

    public static string CombineAndNormalize(params string?[] parts)
        => Normalize(string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))));

    private static bool IsArabicDiacritic(char ch) =>
        ch is (>= '\u064B' and <= '\u065F') or '\u0670' or (>= '\u06D6' and <= '\u06ED');
}
