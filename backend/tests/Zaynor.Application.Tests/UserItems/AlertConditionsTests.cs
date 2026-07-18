using Zaynor.Application.UserItems;

namespace Zaynor.Application.Tests.UserItems;

public class AlertConditionsTests
{
    [Fact]
    public void BuildPriceDropBelow_WithBaselineAndCurrency_RoundTripsThroughParser()
    {
        var condition = AlertConditions.BuildPriceDropBelow(4237.52m, "SAR");

        Assert.Equal("price_drop_below:4237.52 SAR", condition);
        Assert.Equal(4237.52m, AlertConditions.TryParseBaseline(condition));
    }

    [Fact]
    public void BuildPriceDropBelow_WithoutBaseline_ProducesUnparseableCondition()
    {
        var condition = AlertConditions.BuildPriceDropBelow(null, null);

        Assert.Equal("price_drop", condition);
        Assert.Null(AlertConditions.TryParseBaseline(condition));
    }

    [Fact]
    public void BuildPriceDropBelow_WithoutCurrency_StillParses()
    {
        var condition = AlertConditions.BuildPriceDropBelow(100m, null);

        Assert.Equal(100m, AlertConditions.TryParseBaseline(condition));
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("price_drop_below:not-a-number SAR")]
    [InlineData("")]
    public void TryParseBaseline_Malformed_ReturnsNull(string condition)
    {
        Assert.Null(AlertConditions.TryParseBaseline(condition));
    }

    [Fact]
    public void BuildTriggered_ProducesTriggeredCondition_ThatIsNoLongerParseable()
    {
        var active = AlertConditions.BuildPriceDropBelow(4237.52m, "SAR");

        var triggered = AlertConditions.BuildTriggered(3999m, "SAR", active);

        Assert.Equal("triggered:3999 SAR;baseline:4237.52 SAR", triggered);
        Assert.True(AlertConditions.IsTriggered(triggered));
        Assert.Null(AlertConditions.TryParseBaseline(triggered));
    }

    [Fact]
    public void IsTriggered_ActiveCondition_IsFalse()
    {
        Assert.False(AlertConditions.IsTriggered("price_drop_below:100 SAR"));
        Assert.False(AlertConditions.IsTriggered("price_drop"));
    }
}
