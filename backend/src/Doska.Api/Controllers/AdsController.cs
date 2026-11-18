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
public sealed class AdsController(AdService adService) : ControllerBase
{
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
