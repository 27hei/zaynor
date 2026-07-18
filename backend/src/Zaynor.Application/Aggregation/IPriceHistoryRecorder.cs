using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Records the prices observed during a live search into PriceHistory
/// (spec Sections 13/15). This is the accumulation that predictive analytics
/// (FR12, "buy now or wait?") will need months of — so it starts with the very
/// first search, not when the feature ships.
///
/// Implementations must fail soft: recording history must never break the
/// search that produced it (spec NFR4).
/// </summary>
public interface IPriceHistoryRecorder
{
    Task RecordAsync(IReadOnlyList<AggregatedOffer> offers, CancellationToken cancellationToken = default);
}
