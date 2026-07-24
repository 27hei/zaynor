using Microsoft.AspNetCore.Mvc;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Api.Controllers;

/// <summary>
/// The search endpoint: the entry point to Zaynor's core flow (spec Section 17).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly ISearchSuggestionService _suggestionService;
    private readonly IPriceHistoryService _priceHistoryService;

    public SearchController(
        IAggregationService aggregationService,
        ISearchSuggestionService suggestionService,
        IPriceHistoryService priceHistoryService)
    {
        _aggregationService = aggregationService;
        _suggestionService = suggestionService;
        _priceHistoryService = priceHistoryService;
    }

    /// <summary>
    /// Searches all configured sources for <paramref name="q"/> and returns a
    /// page of the aggregated, ranked offers with a recommendation. Every
    /// vendor is only ever called once per distinct query regardless of how
    /// many pages get requested — see CachedAggregationService.
    /// </summary>
    /// <param name="q">The product to search for, e.g. "Sony PlayStation 5".</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Offers per page, clamped to 1-50 (default 20).</param>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResult>> Search(
        [FromQuery] string q, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "A search term ('q') is required." });
        }

        var result = await _aggregationService.SearchAsync(
            q, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Autocomplete suggestions drawn from products Zaynor has seen
    /// (competitive analysis table stakes #1). Returns an empty list for
    /// inputs under two characters.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> Suggestions(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<string>());
        }

        return Ok(await _suggestionService.GetSuggestionsAsync(q, limit: 8, cancellationToken));
    }

    /// <summary>
    /// The accumulated price history for a product (competitive analysis
    /// table stakes #5). Empty until searches have recorded observations.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PriceHistoryResponse>> History(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new PriceHistoryResponse { ProductName = null, Points = Array.Empty<PriceHistoryPoint>() });
        }

        return Ok(await _priceHistoryService.GetHistoryAsync(q, cancellationToken));
    }
}
