using System.Security.Cryptography;
using System.Text;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Signs/verifies outbound store URLs with HMAC-SHA256 so <c>/api/out</c> can
/// safely redirect to any domain a search result legitimately points at —
/// including the open-ended, unpredictable set of real merchants the
/// Immersive Product API resolves (Mazeed, LetsTango, desertcart, ...) —
/// without becoming an open redirector for attacker-supplied URLs. A valid
/// signature proves the URL is exactly what <see cref="AggregationService"/>
/// put in a real search result; nothing else can produce one without the key.
/// </summary>
public static class OutboundLinkSigner
{
    public static string Sign(string url, string key)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(url));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static bool Verify(string url, string? signature, string key)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var expected = Sign(url, key);
        var actual = Encoding.UTF8.GetBytes(signature);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        // Constant-time comparison — this is a security boundary check.
        return actual.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(actual, expectedBytes);
    }
}
