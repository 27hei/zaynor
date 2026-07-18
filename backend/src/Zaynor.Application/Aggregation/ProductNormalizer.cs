using System.Globalization;
using System.Text;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Produces a normalized matching key from a product title so that listings
/// from different stores (and different languages/casing/punctuation) can be
/// recognized as the same product (spec FR3). This is a deliberately simple
/// first version; smarter matching (synonyms, models, AI) comes later.
/// </summary>
public static class ProductNormalizer
{
    /// <summary>
    /// Lower-cases, strips diacritics and punctuation, and collapses whitespace,
    /// yielding a stable key. E.g. "Sony PlayStation 5!" → "sony playstation 5".
    /// </summary>
    public static string Normalize(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var lowered = title.Trim().ToLowerInvariant();
        var decomposed = lowered.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var ch in decomposed)
        {
            // Drop combining marks (diacritics) left by decomposition.
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasSpace = false;
            }
            else if (!lastWasSpace && builder.Length > 0)
            {
                // Any run of punctuation/whitespace becomes a single space.
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim().Normalize(NormalizationForm.FormC);
    }
}
