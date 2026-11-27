using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Abstractions;
using Doska.Application.Ads;
using Doska.Application.Common;
using Doska.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Doska.Api.Controllers;

/// <summary>Подача, ведение и поиск объявлений.</summary>
[ApiController]
[Route("api/ads")]
[Authorize]
public sealed class AdsController(AdService adService, AdSearchService adSearchService) : ControllerBase
{
    private bool CanModerate =>
        User.IsInRole(nameof(UserRole.Moderator)) || User.IsInRole(nameof(UserRole.Admin));

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

    /// <summary>Создаёт объявление с фотографиями от имени текущего пользователя.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<ActionResult<AdResponse>> Create(
        [FromForm] CreateAdForm form,
        CancellationToken cancellationToken)
    {
        var command = new CreateAdCommand(
            form.Title,
            form.Description,
            form.Price,
            form.CityId,
            form.CategoryId,
            form.Contacts);

        var photos = new List<PhotoUpload>(form.Photos.Count);
        var streams = new List<Stream>(form.Photos.Count);

        try
        {
            if (form.Photos.Count is < AdService.MinPhotos or > AdService.MaxPhotos)
            {
                throw new ServiceException(
                    ServiceErrorKind.Validation,
                    $"Приложите от {AdService.MinPhotos} до {AdService.MaxPhotos} фотографий.");
            }

            foreach (var file in form.Photos)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                photos.Add(new PhotoUpload(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length));
            }

            var ad = await adService.CreateAsync(User.GetRequiredUserId(), command, photos, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, AdResponse.From(ad));
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>Возвращает объявления текущего пользователя.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<AdResponse>>> Mine(CancellationToken cancellationToken)
    {
        var ads = await adService.GetMineAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ads.Select(AdResponse.From).ToList());
    }

    /// <summary>Возвращает активное объявление любому посетителю, а иное — автору или модератору.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<AdResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ad = await adService.GetByIdAsync(
            id,
            User.GetUserIdOrNull(),
            CanModerate,
            cancellationToken);

        return Ok(AdResponse.From(ad));
    }

    /// <summary>Изменяет объявление автора.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdResponse>> Update(
        Guid id,
        [FromBody] UpdateAdRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAdCommand(
            request.Title,
            request.Description,
            request.Price,
            request.CityId,
            request.CategoryId,
            request.Contacts);

        var ad = await adService.UpdateAsync(
            id,
            User.GetRequiredUserId(),
            command,
            cancellationToken);

        return Ok(AdResponse.From(ad));
    }

    /// <summary>Удаляет объявление автора.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await adService.DeleteAsync(id, User.GetRequiredUserId(), cancellationToken);
        return NoContent();
    }

    /// <summary>Отмечает объявление проданным.</summary>
    [HttpPost("{id:guid}/sold")]
    public async Task<ActionResult<AdResponse>> MarkSold(Guid id, CancellationToken cancellationToken)
    {
        var ad = await adService.MarkSoldAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(AdResponse.From(ad));
    }

    /// <summary>Возвращает проданное объявление в продажу.</summary>
    [HttpPost("{id:guid}/return-to-sale")]
    public async Task<ActionResult<AdResponse>> ReturnToSale(Guid id, CancellationToken cancellationToken)
    {
        var ad = await adService.ReturnToSaleAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(AdResponse.From(ad));
    }

    /// <summary>Продлевает активное или архивное объявление.</summary>
    [HttpPost("{id:guid}/renew")]
    public async Task<ActionResult<AdResponse>> Renew(Guid id, CancellationToken cancellationToken)
    {
        var ad = await adService.RenewAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(AdResponse.From(ad));
    }
}