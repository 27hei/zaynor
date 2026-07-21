namespace Zaynor.Application.Aggregation;

/// <summary>
/// Mirrors <c>OutController</c>'s exact host-matching rules (kept in sync
/// deliberately — both must agree on what "monetized" means), so the offer
/// badge shown to visitors is never wrong about whether a click through it
/// actually earns commission right now.
/// </summary>
public static class AffiliateEligibility
{
    public static bool IsMonetized(string productUrl, AffiliateSettings settings)
    {
        if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (settings.AmazonTagConfigured
            && (uri.Host == "amazon.sa" || uri.Host.EndsWith(".amazon.sa", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (settings.NoonSuffixConfigured && (uri.Host == "noon.com" || uri.Host == "www.noon.com"))
        {
            return true;
        }

        if (settings.DeeplinkConfigured
            && settings.DeeplinkHosts.Any(h => uri.Host == h || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }
}
