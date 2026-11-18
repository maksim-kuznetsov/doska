using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Управление разделами и подразделами каталога.</summary>
[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminCategoriesController(CategoryService categoryService) : ControllerBase
{
    /// <summary>Полное дерево категорий для администратора.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategorySectionResponse>>> Tree(CancellationToken cancellationToken)
    {
        var sections = await categoryService.GetAdminTreeAsync(cancellationToken);
        return Ok(sections.Select(CategorySectionResponse.From).ToList());
    }

    /// <summary>Создаёт раздел верхнего уровня.</summary>
    [HttpPost("sections")]
    public async Task<ActionResult<CategorySectionResponse>> CreateSection(
        [FromBody] CreateSectionRequest request,
        CancellationToken cancellationToken)
    {
        var section = await categoryService.CreateSectionAsync(
            request.Name,
            request.SortOrder,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            CategorySectionResponse.From(section));
    }

    /// <summary>Создаёт подраздел в разделе.</summary>
    [HttpPost("{id:guid}/subsections")]
    public async Task<ActionResult<CategoryItemResponse>> CreateSubsection(
        Guid id,
        [FromBody] CreateSubsectionRequest request,
        CancellationToken cancellationToken)
    {
        var subsection = await categoryService.CreateSubsectionAsync(
            id,
            request.Name,
            request.SortOrder,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, CategoryItemResponse.From(subsection));
    }

    /// <summary>Переименовывает категорию и меняет порядок.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryItemResponse>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.UpdateAsync(
            id,
            request.Name,
            request.SortOrder,
            cancellationToken);

        return Ok(CategoryItemResponse.From(category));
    }

    /// <summary>Делает категорию доступной для выбора.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<CategoryItemResponse>> Activate(Guid id, CancellationToken cancellationToken)
        => Ok(CategoryItemResponse.From(await categoryService.ActivateAsync(id, cancellationToken)));

    /// <summary>Скрывает категорию из выбора.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<CategoryItemResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
        => Ok(CategoryItemResponse.From(await categoryService.DeactivateAsync(id, cancellationToken)));

    /// <summary>Удаляет пустую категорию.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}