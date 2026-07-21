using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.ImageSearch;

namespace Zaynor.Infrastructure.ImageSearch;

/// <summary>
/// Resolves a photo to a product name via Serper's Google Lens (reverse
/// image search) endpoint — the same Serper account already used for
/// <see cref="Zaynor.Infrastructure.DataSources.GoogleShoppingDataSource"/>,
/// so no separate signup/key is needed. Takes the top visual match's title
/// as the derived query, then the caller runs that through the normal
/// aggregation pipeline — image search never shows Lens's own raw links,
/// only real price comparisons from Zaynor's own sources.
///
/// Config-only activation: dormant until DataSources:Serper:ApiKey is set
/// (the same key GoogleShoppingDataSource uses).
/// </summary>
public sealed class SerperLensQueryResolver : IImageQueryResolver
{
    private const string Endpoint = "https://google.serper.dev/lens";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SerperLensQueryResolver> _logger;
    private readonly string? _apiKey;

    public SerperLensQueryResolver(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SerperLensQueryResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:Serper:ApiKey"];
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string?> ResolveQueryAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SerperLensQueryResolver));
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new { url = imageUrl }),
            };
            request.Headers.Add("X-API-KEY", _apiKey);

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content.ReadFromJsonAsync<LensEnvelope>(cancellationToken: cancellationToken);
            var best = envelope?.Organic?.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.Title));

            return best?.Title?.Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Google Lens lookup failed for image {ImageUrl}", imageUrl);
            return null;
        }
    }

    private sealed record LensEnvelope(
        [property: JsonPropertyName("organic")] List<LensOrganicResult>? Organic);

    private sealed record LensOrganicResult(
        [property: JsonPropertyName("title")] string? Title);
}
