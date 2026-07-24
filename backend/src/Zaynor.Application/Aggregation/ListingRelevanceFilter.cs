using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Precision filters originally built inside GoogleShoppingDataSource after
/// real observed failures — off-topic listings, accessories/repair-parts
/// matching on a model number rather than being the product itself, and
/// wrong-currency/mismatched price outliers. Extracted so every source can
/// apply the same filtering once per-source result caps rise above 1 (a
/// source that only ever returned its single best guess never needed this;
/// one now returning up to N candidates does). Only ever removes items —
/// never invents or edits data.
/// </summary>
public static class ListingRelevanceFilter
{
    private static readonly string[] StopWords = ["a", "an", "the", "for", "of", "with", "and", "in", "on", "to", "by"];
    private static readonly char[] TokenSeparators = [' ', '-', '_', ',', '.', '/', '(', ')'];

    private static readonly string[] AccessoryKeywords =
    [
        "case", "cover", "skin", "screen protector", "tempered glass", "pouch",
        "holder", "stand", "strap", "charger", "cable", "adapter", "sticker",
        "protector", "bumper", "sleeve", "mount", "stylus", "earphone", "earbud",
        "headphone", "power bank", "memory card", "sim card",
        // Repair parts/services — a different failure mode than accessories,
        // but the same root cause (matched on model name, not the product).
        "housing", "back panel", "rear panel", "rear housing", "back cover",
        "battery", "lcd", "display unit", "digitizer", "touch screen", "flex cable",
        "replacement screen", "screen assembly", "screen replacement",
        "motherboard", "mainboard", "logic board", "charging port", "dock connector",
        "camera lens", "repair service", "repair kit", "inspection service",
        "spare part", "spare parts", "sim tray", "rear glass", "back glass",
        // Apparel/merch bundled in by resellers (e.g. a console search
        // surfacing a "T-Shirt & Controller" bundle) — unambiguous, since
        // no genuine device listing mentions clothing. Deliberately not
        // excluding bare "controller": a real "Console with Wireless
        // Controller" bundle listing would be wrongly caught by that, since
        // controllers are a genuine part of many authentic console bundles.
        "shirt", "hoodie", "keychain", "poster",
        // Arabic equivalents — expanded after a real leak: searching
        // "سامسونج A70" (the standard-spelling Arabic query) returned six
        // offers, and every single one was a screen protector, SIM tray,
        // back cover, battery, or replacement screen.
        "غطاء", "غطا", "واقي", "واق", "قطعة غيار", "قطع غيار", "صيانة", "اصلاح", "إصلاح", "كفر", "شاحن",
        // "مدخل"/"فتحة" (slot/port) rather than "شريحة" (SIM) itself — a
        // genuine phone listing legitimately says "ثنائي الشريحة" (dual-SIM)
        // as a feature, so excluding on bare "شريحة" would wrongly catch
        // real phones too; the hardware-port word is what's actually unique
        // to the SIM-tray-as-a-spare-part listings.
        "شاشة", "بطارية", "عدسة", "مدخل", "فتحة", "لاصقة", "زجاج مقوى",
        "جراب", "حافظة", "كابل", "زجاج خلفي", "تيشرت", "قميص",
    ];

    /// <summary>
    /// True when most of the query's meaningful words appear in the title —
    /// a simple, honest precision filter for loosely-"related" items that
    /// share no real overlap with what was actually searched.
    /// </summary>
    public static bool IsRelevant(string query, string title)
    {
        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0)
        {
            return true;
        }

        var titleTokens = Tokenize(title);
        var matches = queryTokens.Count(qt => titleTokens.Any(tt => TokensMatch(qt, tt)));

        // A short "brand + model" query (the most common shape) must match on
        // every word — e.g. "Samsung A70" (2 tokens) must not let an
        // unrelated "itel A70" phone through on a shared model number alone.
        // Longer queries keep majority-rounded-up slack, since real-world
        // title wording varies more the more it's asked.
        var required = queryTokens.Count <= 2 ? queryTokens.Count : (queryTokens.Count + 1) / 2;
        return matches >= Math.Max(1, required);
    }

    /// <summary>
    /// True when the title is clearly an accessory FOR the searched product
    /// rather than the product itself — unless the query is itself looking
    /// for that accessory (e.g. a genuine "iphone 15 case" search). Checked
    /// against both the original and brand-normalized query text, since
    /// either could be the one carrying the accessory word the user typed.
    /// </summary>
    public static bool IsAccessoryMismatch(string query, string effectiveQuery, string title)
    {
        var q1 = query.ToLowerInvariant();
        var q2 = effectiveQuery.ToLowerInvariant();

        // If the query itself signals accessory intent, trust it wholesale —
        // a title using a different accessory synonym ("case" vs. "cover")
        // than the query shouldn't still get excluded.
        if (AccessoryKeywords.Any(keyword => q1.Contains(keyword, StringComparison.Ordinal) || q2.Contains(keyword, StringComparison.Ordinal)))
        {
            return false;
        }

        var titleLower = title.ToLowerInvariant();
        return AccessoryKeywords.Any(keyword => titleLower.Contains(keyword, StringComparison.Ordinal));
    }

    /// <summary>
    /// Drops offers priced far outside the cluster's own median — a genuine
    /// order-of-magnitude anomaly (a mislabeled accessory, a wrong-currency
    /// parse), not a real price. With fewer than 3 offers there isn't enough
    /// signal to safely judge a cluster, so nothing is removed.
    /// </summary>
    public static List<StoreOffer> RemovePriceOutliers(List<StoreOffer> offers)
    {
        if (offers.Count < 3)
        {
            return offers;
        }

        var sorted = offers.Select(o => o.Price).OrderBy(p => p).ToList();
        var mid = sorted.Count / 2;
        var median = sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
        var lowerBound = median * 0.2m;
        var upperBound = median * 8m;

        return offers.Where(o => o.Price >= lowerBound && o.Price <= upperBound).ToList();
    }

    /// <summary>
    /// Exact match, or — only for alphabetic words of at least 4 letters — a
    /// prefix match to tolerate simple plurals/variants ("console"/
    /// "consoles"). Deliberately excludes short/numeric tokens ("5", "15"),
    /// since substring containment there would wrongly match "5" against
    /// "55" or "2500".
    /// </summary>
    private static bool TokensMatch(string a, string b)
    {
        if (a == b)
        {
            return true;
        }

        if (a.Length < 4 || b.Length < 4 || !a.All(char.IsLetter) || !b.All(char.IsLetter))
        {
            return false;
        }

        return a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal);
    }

    private static List<string> Tokenize(string text) =>
        text
            .ToLowerInvariant()
            .Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !StopWords.Contains(w))
            .Distinct()
            .ToList();
}
