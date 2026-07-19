using Microsoft.AspNetCore.Mvc;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Api.Controllers;

/// <summary>Covered products for real category browsing (spec FR10).</summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly CuratedProductDataSource _catalog;

    public CatalogController(CuratedProductDataSource catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CuratedProductDataSource.CatalogSummary>> List() =>
        Ok(_catalog.GetSummaries());
}
