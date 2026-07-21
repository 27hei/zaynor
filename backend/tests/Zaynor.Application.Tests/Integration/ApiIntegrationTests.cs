using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Zaynor.Application.Aggregation.Models;
using Zaynor.Application.Auth.Models;
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
    public async Task Search_UncoveredProduct_IsFlaggedAsDemo()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<SearchResult>("/api/search?q=xbox");

        Assert.NotNull(result);
        Assert.True(result!.IsDemoData);
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
}
