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

    public SearchController(IAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    /// <summary>
    /// Searches all configured sources for <paramref name="q"/> and returns the
    /// aggregated offers (cheapest first) with a recommendation.
    /// </summary>
    /// <param name="q">The product to search for, e.g. "Sony PlayStation 5".</param>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResult>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "A search term ('q') is required." });
        }

        var result = await _aggregationService.SearchAsync(q, cancellationToken);
        return Ok(result);
    }
}
