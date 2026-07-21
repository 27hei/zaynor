namespace Zaynor.Application.Aggregation;

/// <summary>
/// Which affiliate mechanisms are actually configured right now — bound
/// once from the same config keys <c>OutController</c> reads to decide
/// whether to tag an outbound link. Kept as plain booleans/hosts (not the
/// secret values themselves) so this can safely reach the aggregation
/// engine, which has no business seeing the actual tag/template strings.
/// </summary>
public sealed class AffiliateSettings
{
    public bool AmazonTagConfigured { get; init; }

    public bool NoonSuffixConfigured { get; init; }

    public bool DeeplinkConfigured { get; init; }

    public IReadOnlyCollection<string> DeeplinkHosts { get; init; } = Array.Empty<string>();
}
