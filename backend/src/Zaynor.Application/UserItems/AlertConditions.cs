using System.Globalization;

namespace Zaynor.Application.UserItems;

/// <summary>
/// The single source of truth for alert condition strings (spec FR8).
/// Formats:
///   "price_drop"                                — no baseline recorded
///   "price_drop_below:4237.52 SAR"              — active, with baseline
///   "triggered:3999.00 SAR;baseline:4237.52 SAR" — fired by the monitor
/// </summary>
public static class AlertConditions
{
    private const string BaselinePrefix = "price_drop_below:";
    private const string TriggeredPrefix = "triggered:";

    public static string BuildPriceDropBelow(decimal? priceBaseline, string? currency)
    {
        if (priceBaseline is null)
        {
            return "price_drop";
        }

        return $"{BaselinePrefix}{FormatAmount(priceBaseline.Value, currency)}";
    }

    public static string BuildTriggered(decimal currentPrice, string? currency, string previousCondition)
    {
        var baseline = previousCondition.StartsWith(BaselinePrefix, StringComparison.Ordinal)
            ? previousCondition[BaselinePrefix.Length..]
            : previousCondition;

        return $"{TriggeredPrefix}{FormatAmount(currentPrice, currency)};baseline:{baseline}";
    }

    /// <summary>Extracts the baseline amount from an active condition, or null when absent/malformed.</summary>
    public static decimal? TryParseBaseline(string condition)
    {
        if (!condition.StartsWith(BaselinePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var payload = condition[BaselinePrefix.Length..];
        var amountText = payload.Split(' ', 2)[0];

        return decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    public static bool IsTriggered(string condition) =>
        condition.StartsWith(TriggeredPrefix, StringComparison.Ordinal);

    private static string FormatAmount(decimal amount, string? currency)
    {
        var text = amount.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency) ? text : $"{text} {currency}";
    }
}
