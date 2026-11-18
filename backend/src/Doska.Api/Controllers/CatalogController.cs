using Doska.Application.Ads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Справочники городов и подразделов категорий.</summary>
[ApiController]
[Route("api/catalog")]
[AllowAnonymous]
public sealed class CatalogController(CatalogService catalogService) : ControllerBase
{
    /// <summary>Активные города.</summary>
    [HttpGet("cities")]
    public async Task<ActionResult<IReadOnlyList<CityOption>>> Cities(CancellationToken cancellationToken)
        => Ok(await catalogService.GetCitiesAsync(cancellationToken));

    /// <summary>Активные подразделы категорий.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryOption>>> Categories(CancellationToken cancellationToken)
        => Ok(await catalogService.GetSubsectionsAsync(cancellationToken));

    /// <summary>Активные разделы верхнего уровня с подразделами.</summary>
    [HttpGet("sections")]
    public async Task<ActionResult<IReadOnlyList<SectionOption>>> Sections(CancellationToken cancellationToken)
        => Ok(await catalogService.GetSectionsAsync(cancellationToken));
}