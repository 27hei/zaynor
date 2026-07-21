namespace Zaynor.Application.Aggregation;

/// <summary>
/// Normalizes common colloquial/informal Arabic spellings of major brand
/// names to their English name — e.g. "سامسنج" (a common everyday spelling
/// of Samsung, missing a "و" versus the standard "سامسونج") to "Samsung".
/// Real observed failure: searching "سامسنج A70" returned nothing at all
/// from Google Shopping and silently fell back to demo data, because Google's
/// own matching didn't map that spelling to Samsung as strongly as the
/// standard one. Merchant listings and Google Shopping's own catalog
/// consistently use each brand's English name regardless of the storefront's
/// display language, so translating before searching (never fabricating
/// data — just correcting a known transliteration) fixes this.
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
    };

    /// <summary>Replaces any known Arabic brand spelling (single- or multi-word) with its English name.</summary>
    public static string Normalize(string query)
    {
        foreach (var (arabic, english) in BrandNames)
        {
            query = query.Replace(arabic, english, StringComparison.Ordinal);
        }

        return query;
    }
}
