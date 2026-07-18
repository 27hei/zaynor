using Zaynor.Application.Aggregation;

namespace Zaynor.Application.Tests.Aggregation;

public class ProductNormalizerTests
{
    [Theory]
    [InlineData("Sony PlayStation 5", "sony playstation 5")]
    [InlineData("  Sony   PlayStation   5  ", "sony playstation 5")]
    [InlineData("Sony PlayStation 5!", "sony playstation 5")]
    [InlineData("SONY playstation 5", "sony playstation 5")]
    public void Normalize_ProducesSameKey_ForEquivalentTitles(string input, string expected)
    {
        Assert.Equal(expected, ProductNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ReturnsEmpty_ForBlankInput(string input)
    {
        Assert.Equal(string.Empty, ProductNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_StripsDiacritics()
    {
        Assert.Equal("cafe", ProductNormalizer.Normalize("Café"));
    }
}
