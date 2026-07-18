using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>Reads the accumulated price history for a product query (FR3-normalized).</summary>
public interface IPriceHistoryService
{
    Task<PriceHistoryResponse> GetHistoryAsync(string query, CancellationToken cancellationToken = default);
}
