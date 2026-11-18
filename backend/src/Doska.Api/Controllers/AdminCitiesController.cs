using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Управление справочником городов; доступно только администратору.</summary>
[ApiController]
[Route("api/admin/cities")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminCitiesController(CityService cityService) : ControllerBase
{
    /// <summary>Полный список городов, включая неактивные.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>> List(CancellationToken cancellationToken)
    {
        var cities = await cityService.GetAdminListAsync(cancellationToken);
        return Ok(cities.Select(CityResponse.From).ToList());
    }

    /// <summary>Добавляет город в справочник.</summary>
    [HttpPost]
    public async Task<ActionResult<CityResponse>> Create(
        [FromBody] CityRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cityService.CreateAsync(request.Name, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, CityResponse.From(city));
    }

    /// <summary>Переименовывает город.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CityResponse>> Rename(
        Guid id,
        [FromBody] CityRequest request,
        CancellationToken cancellationToken)
        => Ok(CityResponse.From(await cityService.RenameAsync(id, request.Name, cancellationToken)));

    /// <summary>Делает город доступным для выбора.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<CityResponse>> Activate(Guid id, CancellationToken cancellationToken)
        => Ok(CityResponse.From(await cityService.ActivateAsync(id, cancellationToken)));

    /// <summary>Скрывает город из выбора.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<CityResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
        => Ok(CityResponse.From(await cityService.DeactivateAsync(id, cancellationToken)));

    /// <summary>Удаляет город без объявлений.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await cityService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}