using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Ads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Doska.Api.Controllers;

/// <summary>Подача объявлений и список собственных объявлений.</summary>
[ApiController]
[Route("api/ads")]
[Authorize]
public sealed class AdsController(AdService adService, AdSearchService adSearchService) : ControllerBase
{
    /// <summary>Публичная лента и поиск по активным объявлениям.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultResponse<AdCardResponse>>> Search(
        [FromQuery] AdSearchRequest request,
        CancellationToken cancellationToken)
    {
        var sort = request.Sort?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => AdSort.PriceAscending,
            "price_desc" => AdSort.PriceDescending,
            _ => AdSort.Newest,
        };

        var query = new AdSearchQuery(
            request.Query,
            request.CategoryId,
            request.CityId,
            request.MinPrice,
            request.MaxPrice,
            sort,
            request.Page ?? 1,
            request.PageSize ?? AdSearchService.DefaultPageSize);

        var result = await adSearchService.SearchAsync(query, cancellationToken);

        return Ok(new PagedResultResponse<AdCardResponse>(
            result.Items.Select(AdCardResponse.From).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }

    /// <summary>Создаёт объявление от имени текущего пользователя.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<ActionResult<AdResponse>> Create(
        [FromBody] CreateAdRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAdCommand(
            request.Title,
            request.Description,
            request.Price,
            request.CityId,
            request.CategoryId,
            request.Contacts);

        var ad = await adService.CreateAsync(User.GetRequiredUserId(), command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, AdResponse.From(ad));
    }

    /// <summary>Возвращает объявления текущего пользователя.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<AdResponse>>> Mine(CancellationToken cancellationToken)
    {
        var ads = await adService.GetMineAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ads.Select(AdResponse.From).ToList());
    }
}
