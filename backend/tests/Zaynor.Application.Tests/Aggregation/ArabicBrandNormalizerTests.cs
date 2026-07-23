using Zaynor.Application.Aggregation;

namespace Zaynor.Application.Tests.Aggregation;

public class ArabicBrandNormalizerTests
{
    [Fact]
    public void NormalizesAColloquialBrandSpelling()
    {
        Assert.Equal("Samsung A70", ArabicBrandNormalizer.Normalize("سامسنج A70"));
    }

    [Fact]
    public void NormalizesAProductTierWord_EvenWithTheStandardBrandSpelling()
    {
        // Real observed failure: even the fully-correct Arabic spelling of
        // Samsung returned zero results, because "الترا" (Ultra) itself
        // isn't matched by Google in Arabic script.
        Assert.Equal("Samsung Ultra", ArabicBrandNormalizer.Normalize("سامسونج الترا"));
    }

    [Fact]
    public void FuzzyMatchesABrandSpellingNotAlreadyInTheDictionary()
    {
        // "سامسنق" (ends in ق) is a real observed spelling distinct from the
        // already-known "سامسنج" (ends in ج) — one edit apart, close enough
        // to correct without having to hardcode every possible variant.
        Assert.Equal("Samsung Ultra", ArabicBrandNormalizer.Normalize("سامسنق الترا"));
    }

    [Fact]
    public void ExactMultiWordPhraseWinsOverFuzzyMatchingIndividualWords()
    {
        Assert.Equal("PlayStation 5", ArabicBrandNormalizer.Normalize("بلاي ستيشن 5"));
    }

    [Fact]
    public void FuzzyMatchesACommonProductNounTypo()
    {
        // Real observed failure: "محفضة" (ض instead of ظ) returned zero
        // results with no correction offered. Unlike brand words, common
        // nouns stay in Arabic — Google matches the correctly-spelled noun
        // fine, so only the spelling itself needs fixing.
        Assert.Equal("محفظة", ArabicBrandNormalizer.Normalize("محفضة"));
    }

    [Fact]
    public void TranslatesAGenericCategoryWord_UnlikeAPlainProductNoun()
    {
        // Real observed failure: "نظارة" returned zero offers even though
        // real listings exist — Google Shopping results for this category
        // skew English/international-brand ("Ray-Ban Aviator Sunglasses"),
        // which the Arabic word never matches. Translating fixes both the
        // Google query and our own title-relevance filter.
        Assert.Equal("Sunglasses", ArabicBrandNormalizer.Normalize("نظارة"));
        Assert.Equal("Sunglasses", ArabicBrandNormalizer.Normalize("نظارات"));
        Assert.Equal("Makeup", ArabicBrandNormalizer.Normalize("مكياج"));
    }

    [Fact]
    public void LeavesUnrelatedArabicTextAlone()
    {
        const string query = "جهاز تكييف عادي";
        Assert.Equal(query, ArabicBrandNormalizer.Normalize(query));
    }

    [Fact]
    public void LeavesEnglishQueriesUnchanged()
    {
        const string query = "Samsung Galaxy A70";
        Assert.Equal(query, ArabicBrandNormalizer.Normalize(query));
    }
}
