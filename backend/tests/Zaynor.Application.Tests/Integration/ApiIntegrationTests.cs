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
        Assert.Contains(result.Offers, o => o.StoreName == "Noon" && o.Price == 3589m);
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
