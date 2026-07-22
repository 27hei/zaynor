using Zaynor.Application.Aggregation;

namespace Zaynor.Application.Tests.Aggregation;

public class OutboundLinkSignerTests
{
    private const string Key = "test-signing-key-0123456789";

    [Fact]
    public void ValidSignature_VerifiesForTheExactUrlAndKeyItWasSignedWith()
    {
        var url = "https://mazeed.sa/products/some-real-listing";
        var sig = OutboundLinkSigner.Sign(url, Key);

        Assert.True(OutboundLinkSigner.Verify(url, sig, Key));
    }

    [Fact]
    public void RejectsASignatureForADifferentUrl()
    {
        // The real threat this guards against: an attacker can't take a
        // legitimately-signed URL's signature and reuse it to smuggle a
        // different (malicious) redirect target past /api/out.
        var sig = OutboundLinkSigner.Sign("https://mazeed.sa/products/real", Key);

        Assert.False(OutboundLinkSigner.Verify("https://evil.example.com/phish", sig, Key));
    }

    [Fact]
    public void RejectsAValidSignatureSignedWithADifferentKey()
    {
        var url = "https://mazeed.sa/products/real";
        var sig = OutboundLinkSigner.Sign(url, "a-different-key");

        Assert.False(OutboundLinkSigner.Verify(url, sig, Key));
    }

    [Fact]
    public void RejectsAMissingSignature()
    {
        Assert.False(OutboundLinkSigner.Verify("https://mazeed.sa/products/real", null, Key));
        Assert.False(OutboundLinkSigner.Verify("https://mazeed.sa/products/real", "", Key));
    }
}
