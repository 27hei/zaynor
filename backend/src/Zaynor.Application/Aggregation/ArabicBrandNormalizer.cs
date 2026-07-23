namespace Zaynor.Application.Aggregation;

/// <summary>
/// Normalizes Arabic-script brand names and product-tier words to what
/// Google Shopping and merchant listings actually use, so real searches
/// stop silently falling back to demo data:
///
/// 1. An exact dictionary of brand names AND common product-tier words
///    ("الترا" → "Ultra", "برو" → "Pro", ...) — real observed failure: even
///    the fully-correct "سامسونج الترا" returned zero results, because
///    Google doesn't match "Ultra" written in Arabic script at all, not
///    just misspelled brand names.
/// 2. Fuzzy (edit-distance) matching against that same dictionary for
///    brand words not already spelled exactly as listed — real observed
///    failure: "سامسنق" (yet another colloquial Samsung spelling, distinct
///    from "سامسنج") wasn't in the dictionary and fell straight through.
///    Enumerating every possible spelling by hand doesn't scale; fuzzy
///    matching against the brands we already know generalizes the fix.
/// </summary>
public static class ArabicBrandNormalizer
{
    private static readonly Dictionary<string, string> BrandNames = new(StringComparer.Ordinal)
    {
        ["سامسنج"] = "Samsung",
        ["سامسونج"] = "Samsung",
        ["سامسوونج"] = "Samsung",
        ["ابل"] = "Apple",
        ["آبل"] = "Apple",
        ["أبل"] = "Apple",
        ["ايفون"] = "iPhone",
        ["آيفون"] = "iPhone",
        ["أيفون"] = "iPhone",
        ["سوني"] = "Sony",
        ["هواوي"] = "Huawei",
        ["شاومي"] = "Xiaomi",
        ["زيومي"] = "Xiaomi",
        ["ريدمي"] = "Redmi",
        ["ريلمي"] = "Realme",
        ["اوبو"] = "Oppo",
        ["أوبو"] = "Oppo",
        ["فيفو"] = "Vivo",
        ["هونر"] = "Honor",
        ["تكنو"] = "Tecno",
        ["انفينكس"] = "Infinix",
        ["إنفينيكس"] = "Infinix",
        ["ايتل"] = "itel",
        ["ون بلس"] = "OnePlus",
        ["وان بلس"] = "OnePlus",
        ["نوكيا"] = "Nokia",
        ["توشيبا"] = "Toshiba",
        ["ديل"] = "Dell",
        ["لينوفو"] = "Lenovo",
        ["ايسوس"] = "Asus",
        ["آسوس"] = "Asus",
        ["اتش بي"] = "HP",
        ["إتش بي"] = "HP",
        ["ال جي"] = "LG",
        ["إل جي"] = "LG",
        ["باناسونيك"] = "Panasonic",
        ["فيليبس"] = "Philips",
        ["بوش"] = "Bosch",
        ["براون"] = "Braun",
        ["كانون"] = "Canon",
        ["نيكون"] = "Nikon",
        ["مايكروسوفت"] = "Microsoft",
        ["جوجل"] = "Google",
        ["بلايستيشن"] = "PlayStation",
        ["بلاي ستيشن"] = "PlayStation",
        ["اكس بوكس"] = "Xbox",
        ["إكس بوكس"] = "Xbox",
        ["نينتندو"] = "Nintendo",
        ["جالكسي"] = "Galaxy",
        ["جالاكسي"] = "Galaxy",

        // Product-tier/model words — not brand-specific, but just as
        // unmatched by Google when left in Arabic script (spec: the
        // "سامسونج الترا" failure above).
        ["الترا"] = "Ultra",
        ["برو"] = "Pro",
        ["ماكس"] = "Max",
        ["بلس"] = "Plus",
        ["ميني"] = "Mini",
        ["نوت"] = "Note",
        ["فولد"] = "Fold",
        ["فليب"] = "Flip",
        ["لايت"] = "Lite",
        ["تاب"] = "Tab",
        ["واتش"] = "Watch",
    };

    /// <summary>Single Arabic-script words short/common enough that fuzzy matching would be unsafe.</summary>
    private static readonly HashSet<string> FuzzyExcluded = new(StringComparer.Ordinal) { "برو", "نوت", "تاب" };

    /// <summary>
    /// Common Arabic product nouns spelled correctly. Unlike <see cref="BrandNames"/>
    /// these are never translated to English — Google Shopping matches correctly-spelled
    /// Arabic product nouns fine (confirmed: "محفظة" returns real results) — the failure
    /// mode here is purely a single-letter typo in ordinary Arabic (e.g. "محفضة" with ض
    /// instead of ظ) producing zero results with no correction offered. Fuzzy-matching
    /// against this list corrects the spelling and keeps the query in Arabic.
    /// </summary>
    private static readonly HashSet<string> CommonProductNouns = new(StringComparer.Ordinal)
    {
        // Fashion / accessories
        "محفظة", "حقيبة", "حذاء", "احذية", "ساعة", "نظارة", "نظارات",
        // Personal care
        "عطر", "عطور", "كريم", "شامبو",
        // Home appliances
        "غسالة", "ثلاجة", "مكيف", "مروحة", "مكنسة", "فرن", "مايكرويف", "خلاط", "مكواة",
        // Electronics / computers
        "جوال", "هاتف", "لابتوب", "حاسوب", "تابلت", "شاشة", "طابعة", "كاميرا", "سماعة", "سماعات",
        "شاحن", "كابل",
        // TV / audio
        "تلفزيون", "تلفاز",
    };

    /// <summary>Replaces any known Arabic term (exact) or a close misspelling of one (fuzzy) with its English form.</summary>
    public static string Normalize(string query)
    {
        // Exact matches first — cheap, and correct for the common case.
        foreach (var (arabic, english) in BrandNames)
        {
            query = query.Replace(arabic, english, StringComparison.Ordinal);
        }

        // Then fuzzy-match whatever Arabic-script words remain (i.e. weren't
        // already replaced above) against the same dictionary, catching
        // spelling variants we haven't seen and hardcoded yet. Words that
        // aren't a brand/tier match (or a close misspelling of one) are then
        // checked against common product nouns instead, correcting ordinary
        // typos ("محفضة" → "محفظة") while staying in Arabic.
        var words = query.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0 || !ContainsArabicScript(words[i]))
            {
                continue;
            }

            var closestBrand = FindClosestBrandTerm(words[i]);
            if (closestBrand is not null)
            {
                words[i] = closestBrand;
                continue;
            }

            var closestNoun = FindClosestTerm(words[i], CommonProductNouns);
            if (closestNoun is not null)
            {
                words[i] = closestNoun;
            }
        }

        return string.Join(' ', words);
    }

    private static string? FindClosestBrandTerm(string word) =>
        FindClosestTerm(word, BrandNames.Keys, arabic => BrandNames[arabic], arabic => arabic.Contains(' ') || FuzzyExcluded.Contains(arabic));

    private static string? FindClosestTerm(string word, IEnumerable<string> candidates) =>
        FindClosestTerm(word, candidates, static c => c, static _ => false);

    private static string? FindClosestTerm(
        string word, IEnumerable<string> candidates, Func<string, string> resultSelector, Func<string, bool> skip)
    {
        string? best = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            // Multi-word phrases ("بلاي ستيشن") can't fuzzy-match a single
            // split word, and very short/common words are too risky to
            // fuzzy-match at all (too easy to coincidentally sit 1 edit
            // away from unrelated Arabic text).
            if (skip(candidate))
            {
                continue;
            }

            var distance = LevenshteinDistance(word, candidate);
            // Scales with word length: an exact/near-exact hit on a longer,
            // more distinctive word is a safe correction; the same edit
            // count on a short word is far more likely a coincidence.
            var threshold = candidate.Length switch
            {
                <= 4 => 0,
                <= 6 => 1,
                _ => 2,
            };

            if (distance <= threshold && distance < bestDistance)
            {
                bestDistance = distance;
                best = resultSelector(candidate);
            }
        }

        return best;
    }

    private static bool ContainsArabicScript(string text) =>
        text.Any(c => c is >= '؀' and <= 'ۿ');

    /// <summary>Classic edit-distance DP — small inputs (single words), so no need for anything fancier.</summary>
    private static int LevenshteinDistance(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            dp[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            dp[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
            }
        }

        return dp[a.Length, b.Length];
    }
}
