using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Zaynor.Application.Aggregation.Models;
using Zaynor.Application.Auth.Models;
using Zaynor.Application.Reviews.Models;
using Zaynor.Application.SiteReviews.Models;
using Zaynor.Application.Support.Models;
using Zaynor.Application.UserItems.Models;

namespace Zaynor.Application.Tests.Integration;

/// <summary>
/// Boots the real API (spec Section 14: integration tests) on an isolated
/// SQLite database and exercises the endpoints end-to-end over HTTP.
/// </summary>
public sealed class ZaynorApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"zaynor-it-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["Jwt:Key"] = "integration-test-signing-key-0123456789-0123456789-0123456789",
                ["AlertMonitor:IntervalMinutes"] = "60",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}

public class ApiIntegrationTests : IClassFixture<ZaynorApiFactory>
{
    private readonly ZaynorApiFactory _factory;

    public ApiIntegrationTests(ZaynorApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_CuratedProduct_ReturnsRealPricesNotDemo()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<SearchResult>("/api/search?q=s24%20ultra");

        Assert.NotNull(result);
        Assert.False(result!.IsDemoData);
        Assert.Contains(result.Offers, o => o.StoreName == "Amazon.sa" && o.Price == 3899m);
        Assert.True(result.Offers.First(o => o.IsLowestPrice).Price <= result.Offers.Max(o => o.Price));
    }

    [Fact]
    public async Task Search_UncoveredProductWithNoLiveSourceConfigured_ReturnsEmptyNotDemo()
    {
        // Demo/mock data was removed outright (spec: no fabricated prices,
        // ever) — a product outside the curated catalog with no live source
        // configured (no Serper key in this test host) must come back
        // genuinely empty, never silently filled in with placeholder offers.
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<SearchResult>("/api/search?q=xbox");

        Assert.NotNull(result);
        Assert.False(result!.IsDemoData);
        Assert.Empty(result.Offers);
    }

    [Fact]
    public async Task Suggestions_AfterASearch_IncludeTheSeenProduct()
    {
        var client = _factory.CreateClient();

        // A search records the product; suggestions read what Zaynor has seen.
        await client.GetAsync("/api/search?q=iphone%2015");
        var suggestions = await client.GetFromJsonAsync<List<string>>("/api/search/suggestions?q=iph");

        Assert.NotNull(suggestions);
        Assert.Contains(suggestions!, s => s.Contains("iPhone 15"));
    }

    [Fact]
    public async Task Auth_RegisterLoginAndMe_FlowWorks()
    {
        var client = _factory.CreateClient();
        var email = $"it-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        Assert.Equal(email, me!.Email);

        var badLogin = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);
    }

    [Fact]
    public async Task Admin_BootstrapPromotesTheConfiguredEmail()
    {
        // No self-service admin registration exists anywhere — the only way
        // to become admin is being the specific email set in Admin:Email
        // config, applied idempotently at startup (Program.cs). /api/auth/me
        // reads IsAdmin fresh from the DB each call, so promotion is visible
        // immediately even though the JWT issued at registration predates it.
        var client = _factory.CreateClient();
        var email = $"it-admin-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var beforePromotion = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        Assert.False(beforePromotion!.IsAdmin);

        using var adminFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Email"] = email })));
        var adminClient = adminFactory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var afterPromotion = await adminClient.GetFromJsonAsync<UserDto>("/api/auth/me");
        Assert.True(afterPromotion!.IsAdmin);
    }

    [Fact]
    public async Task Outbound_KnownStore_LogsAndRedirects()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var url = "https://www.noon.com/saudi-en/test";
        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=Noon&product=Test");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(url, response.Headers.Location!.ToString());

        var stats = await client.GetFromJsonAsync<Dictionary<string, int>>("/api/out/stats");
        Assert.True(stats!["totalClicks"] >= 1);
    }

    [Fact]
    public async Task Outbound_EbayLink_Redirects()
    {
        // Real reported bug: GoogleShoppingDataSource started building direct
        // ebay.com search links (instead of Google's fragile compare-prices
        // panel), but /api/out's host allowlist hadn't been updated to
        // match — every eBay click came back "Unknown store link." instead
        // of reaching the store.
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var url = "https://www.ebay.com/sch/i.html?_nkw=iphone+15";
        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=eBay&product=Test");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(url, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Outbound_SignedUnknownDomain_Redirects()
    {
        // The Immersive Product API resolves real, working links to an
        // open-ended set of merchants (Mazeed, LetsTango, desertcart, ...)
        // that can never all be on the static AllowedHosts list — a valid
        // signature (computed the same way GoogleShoppingDataSource does)
        // is what proves this specific URL came from a real search result
        // rather than an attacker-supplied redirect target.
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        const string signingKey = "integration-test-signing-key-0123456789-0123456789-0123456789";
        var url = "https://mazeed.sa/products/some-real-listing";
        var sig = Zaynor.Application.Aggregation.OutboundLinkSigner.Sign(url, signingKey);

        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=Mazeed&product=Test&sig={Uri.EscapeDataString(sig)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(url, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Outbound_UnsignedUnknownDomain_IsStillRejected()
    {
        // Without a valid signature, an unrecognized domain must still be
        // rejected exactly as before — the signature is an addition, not a
        // relaxation, of the open-redirect protection.
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString("https://mazeed.sa/products/some-real-listing")}&store=Mazeed&product=Test");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Outbound_AmazonLink_CarriesTheAssociatesTag()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var url = "https://www.amazon.sa/-/en/s?k=ps5";
        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=Amazon.sa&product=PS5");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("tag=zaynor-21", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Outbound_AmazonLinkWithDibTag_StillGetsTheAssociatesTag()
    {
        // Live Amazon search URLs carry dib_tag=se, which contains "tag=" as a
        // substring but is NOT the Associates tag. Our tag must still be added.
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var url = "https://www.amazon.sa/product/dp/B0BKLC5MTT/ref=sr_1_3?dib_tag=se&keywords=gaming+keyboard";
        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=Amazon.sa&product=Keyboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("tag=zaynor-21", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Outbound_DeeplinkTemplate_WrapsNetworkStoreLinks()
    {
        // Derived factory: deeplink template configured (as it will be once a
        // network approves) — Jarir links must ride inside the tracking link.
        // (Noon isn't in the default deeplink hosts — it gets its own direct
        // UTM tagging below, since noon.partners tags same-domain links
        // rather than wrapping them behind a redirector.)
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Affiliate:DeeplinkTemplate"] = "https://ad.admitad.com/g/test123/?ulp={url}",
                })));
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var url = "https://www.jarir.com/sa-en/some-product.html";
        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=Jarir&product=Test");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://ad.admitad.com/g/test123/?ulp=", location);
        Assert.Contains(Uri.EscapeDataString(url), location);
    }

    [Fact]
    public async Task Outbound_NoonLink_CarriesTheUtmTrackingSuffix()
    {
        // Noon (noon.partners) tags per-URL via query params appended
        // directly to the noon.com link itself — not a redirector wrap.
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Affiliate:NoonUtmSuffix"] =
                        "utm_campaign=CMPtest&utm_medium=AFFtest&adjust_deeplink_js=1&utm_source=Ctest",
                })));
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var url = "https://www.noon.com/saudi-en/some-product/p/";
        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString(url)}&store=Noon&product=Test");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith(url + "?", location);
        Assert.Contains("utm_campaign=CMPtest", location);
        Assert.Contains("utm_medium=AFFtest", location);
    }

    [Fact]
    public async Task Outbound_UnknownDomain_IsRejected()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(
            $"/api/out?u={Uri.EscapeDataString("https://evil.example.com/phish")}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SavedProducts_RequireAuthentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/saved");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SavedProducts_FullCycle_CreateListDelete()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"it-{Guid.NewGuid():N}@test.local", password = "password123", locale = "en" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);

        var saved = await client.PostAsJsonAsync("/api/saved", new { productName = "PlayStation 5" });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var dto = await saved.Content.ReadFromJsonAsync<SavedProductDto>();

        var list = await client.GetFromJsonAsync<List<SavedProductDto>>("/api/saved");
        Assert.Contains(list!, s => s.Id == dto!.Id);

        var delete = await client.DeleteAsync($"/api/saved/{dto!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await client.GetFromJsonAsync<List<SavedProductDto>>("/api/saved");
        Assert.DoesNotContain(after!, s => s.Id == dto.Id);
    }

    [Fact]
    public async Task Reviews_PublicList_RequiresNoAuth()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/reviews?storeName=NeverReviewed-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reviews = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();
        Assert.Empty(reviews!);
    }

    [Fact]
    public async Task Reviews_SubmitRequiresAuth()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reviews", new { storeName = "Amazon.sa", rating = 5, comment = "Great!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reviews_FullCycle_SubmitThenListShowsIt_EvenWhenNegative()
    {
        // Founder's explicit call: reviews are never hidden for being
        // negative — a 1-star review must show up exactly like a 5-star one.
        var client = _factory.CreateClient();
        var email = $"it-review-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var storeName = $"TestStore-{Guid.NewGuid():N}";
        var submit = await client.PostAsJsonAsync(
            "/api/reviews", new { storeName, rating = 1, comment = "Terrible delivery experience.", displayName = "Unhappy Customer" });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var list = await client.GetFromJsonAsync<List<ReviewDto>>($"/api/reviews?storeName={storeName}");
        var review = Assert.Single(list!);
        Assert.Equal(1, review.Rating);
        Assert.Equal("Terrible delivery experience.", review.Comment);
        Assert.Equal("Unhappy Customer", review.DisplayName);
        Assert.Null(review.AdminReply);
    }

    [Fact]
    public async Task Reviews_CaseInsensitiveStoreNameMatch_DoesNotFragmentReviews()
    {
        var client = _factory.CreateClient();
        var email = $"it-review-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var baseName = $"CaseTest-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/reviews", new { storeName = baseName.ToLowerInvariant(), rating = 5, comment = "First." });
        await client.PostAsJsonAsync("/api/reviews", new { storeName = baseName.ToUpperInvariant(), rating = 4, comment = "Second." });

        var list = await client.GetFromJsonAsync<List<ReviewDto>>($"/api/reviews?storeName={baseName}");
        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task Reviews_AdminReply_RequiresAdminRole_AndAppearsPublicly()
    {
        var client = _factory.CreateClient();
        var email = $"it-review-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var storeName = $"ReplyTest-{Guid.NewGuid():N}";
        var submit = await client.PostAsJsonAsync("/api/reviews", new { storeName, rating = 2, comment = "Slow shipping." });
        var review = await submit.Content.ReadFromJsonAsync<ReviewDto>();

        var forbidden = await client.PostAsJsonAsync($"/api/admin/reviews/{review!.Id}/reply", new { reply = "Sorry about that!" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var adminFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Email"] = email })));
        var adminClient = adminFactory.CreateClient();

        // The token from registration predates promotion and has no Admin
        // role claim — [Authorize(Roles = "Admin")] checks the JWT's claims,
        // not a fresh DB read, so a new login (issued after the bootstrap
        // above promoted this email) is required to get a token that
        // actually carries the role.
        var relogin = await adminClient.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });
        var adminAuth = await relogin.Content.ReadFromJsonAsync<AuthResponse>();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.Token);

        var reply = await adminClient.PostAsJsonAsync($"/api/admin/reviews/{review.Id}/reply", new { reply = "Sorry about that!" });
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);

        var publicList = await client.GetFromJsonAsync<List<ReviewDto>>($"/api/reviews?storeName={storeName}");
        Assert.Equal("Sorry about that!", Assert.Single(publicList!).AdminReply);
    }

    [Fact]
    public async Task SupportTickets_RequireAuthentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/support/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SupportTickets_AdminEndpoints_RejectNonAdmin()
    {
        var client = _factory.CreateClient();
        var email = $"it-support-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var response = await client.GetAsync("/api/admin/support/tickets");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SupportTickets_OwnershipIsolation_UserCannotSeeAnotherUsersTicket()
    {
        var clientA = _factory.CreateClient();
        var emailA = $"it-support-a-{Guid.NewGuid():N}@test.local";
        var registerA = await clientA.PostAsJsonAsync(
            "/api/auth/register", new { email = emailA, password = "password123", locale = "ar" });
        var authA = await registerA.Content.ReadFromJsonAsync<AuthResponse>();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authA!.Token);

        var created = await clientA.PostAsJsonAsync(
            "/api/support/tickets", new { subject = "Order issue", message = "My order hasn't arrived." });
        var ticket = await created.Content.ReadFromJsonAsync<SupportTicketDto>();

        var clientB = _factory.CreateClient();
        var emailB = $"it-support-b-{Guid.NewGuid():N}@test.local";
        var registerB = await clientB.PostAsJsonAsync(
            "/api/auth/register", new { email = emailB, password = "password123", locale = "ar" });
        var authB = await registerB.Content.ReadFromJsonAsync<AuthResponse>();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authB!.Token);

        var response = await clientB.GetAsync($"/api/support/tickets/{ticket!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SupportTickets_FullCycle_CreateReplyCloseThenCustomerReplyReopens()
    {
        var client = _factory.CreateClient();
        var email = $"it-support-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var created = await client.PostAsJsonAsync(
            "/api/support/tickets", new { subject = "Refund question", message = "How do I get a refund?" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var ticket = await created.Content.ReadFromJsonAsync<SupportTicketDto>();
        Assert.False(ticket!.IsClosed);

        using var adminFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Email"] = email })));
        var adminClient = adminFactory.CreateClient();
        var relogin = await adminClient.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });
        var adminAuth = await relogin.Content.ReadFromJsonAsync<AuthResponse>();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.Token);

        // Admin sees it in the inbox and replies.
        var allTickets = await adminClient.GetFromJsonAsync<List<AdminSupportTicketDto>>("/api/admin/support/tickets");
        Assert.Contains(allTickets!, t => t.Id == ticket.Id && t.UserEmail == email);

        var reply = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/tickets/{ticket.Id}/messages", new { body = "Refunds take 3-5 business days." });
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);

        // Customer sees the reply.
        var thread = await client.GetFromJsonAsync<SupportTicketDetailDto>($"/api/support/tickets/{ticket.Id}");
        Assert.Contains(thread!.Messages, m => m.IsFromAdmin && m.Body == "Refunds take 3-5 business days.");

        // Admin closes it.
        var close = await adminClient.PostAsync($"/api/admin/support/tickets/{ticket.Id}/close", null);
        Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);
        var afterClose = await client.GetFromJsonAsync<SupportTicketDetailDto>($"/api/support/tickets/{ticket.Id}");
        Assert.True(afterClose!.IsClosed);

        // A customer reply reopens it — no dead-end support experience.
        var followUp = await client.PostAsJsonAsync(
            $"/api/support/tickets/{ticket.Id}/messages", new { body = "Thanks, one more question." });
        Assert.Equal(HttpStatusCode.OK, followUp.StatusCode);
        var afterReply = await client.GetFromJsonAsync<SupportTicketDetailDto>($"/api/support/tickets/{ticket.Id}");
        Assert.False(afterReply!.IsClosed);
    }

    [Fact]
    public async Task SiteReviews_PublicList_RequiresNoAuth()
    {
        var response = await _factory.CreateClient().GetAsync("/api/site-reviews");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SiteReviews_SubmitRequiresAuth()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/site-reviews", new { rating = 5, comment = "Great site!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SiteReviews_FullCycle_SubmitThenListShowsIt_EvenWhenNegative()
    {
        var client = _factory.CreateClient();
        var email = $"it-sitereview-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var submit = await client.PostAsJsonAsync(
            "/api/site-reviews", new { rating = 2, comment = "The search results are sometimes slow.", displayName = "Honest User" });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var created = await submit.Content.ReadFromJsonAsync<SiteReviewDto>();

        var list = await client.GetFromJsonAsync<List<SiteReviewDto>>("/api/site-reviews");
        Assert.Contains(list!, r => r.Id == created!.Id && r.Rating == 2 && r.DisplayName == "Honest User");
    }

    [Fact]
    public async Task SiteReviews_Delete_RequiresAdminRole_AndRemovesItPublicly()
    {
        var client = _factory.CreateClient();
        var email = $"it-sitereview-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "password123", locale = "ar" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var submit = await client.PostAsJsonAsync(
            "/api/site-reviews", new { rating = 1, comment = "Insulting spam comment." });
        var created = await submit.Content.ReadFromJsonAsync<SiteReviewDto>();

        // Non-admin cannot delete.
        var forbidden = await client.DeleteAsync($"/api/site-reviews/{created!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var adminFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Email"] = email })));
        var adminClient = adminFactory.CreateClient();
        var relogin = await adminClient.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });
        var adminAuth = await relogin.Content.ReadFromJsonAsync<AuthResponse>();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.Token);

        var delete = await adminClient.DeleteAsync($"/api/site-reviews/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<List<SiteReviewDto>>("/api/site-reviews");
        Assert.DoesNotContain(afterDelete!, r => r.Id == created.Id);
    }
}
