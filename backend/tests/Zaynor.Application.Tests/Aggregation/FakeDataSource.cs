using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Tests.Aggregation;

/// <summary>A configurable test double for <see cref="IProductDataSource"/>.</summary>
internal sealed class FakeDataSource : IProductDataSource
{
    private readonly IReadOnlyList<StoreOffer> _offers;
    private readonly Exception? _throws;

    private FakeDataSource(IReadOnlyList<StoreOffer> offers, Exception? throws)
    {
        _offers = offers;
        _throws = throws;
    }

    public string SourceName => "Fake";

    public static FakeDataSource Returning(params StoreOffer[] offers) => new(offers, null);

    public static FakeDataSource Throwing(Exception exception) => new(Array.Empty<StoreOffer>(), exception);

    public Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_throws is not null)
        {
            throw _throws;
        }

        return Task.FromResult(_offers);
    }

    public static StoreOffer Offer(string store, decimal price) => new()
    {
        StoreName = store,
        ProductTitle = "Test Product",
        Price = price,
        Currency = "SAR",
        ProductUrl = $"https://example.com/{store}",
    };
}
