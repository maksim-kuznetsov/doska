using Doska.Application.Categories;

namespace Doska.Api.Contracts;

/// <summary>Запрос создания раздела.</summary>
public sealed record CreateSectionRequest(string Name, int SortOrder);

/// <summary>Запрос создания подраздела.</summary>
public sealed record CreateSubsectionRequest(string Name, int SortOrder);

/// <summary>Запрос переименования и изменения порядка.</summary>
public sealed record UpdateCategoryRequest(string Name, int SortOrder);

/// <summary>Подраздел в административном дереве.</summary>
public sealed record CategoryItemResponse(Guid Id, string Name, int SortOrder, bool IsActive)
{
    /// <summary>Создаёт ответ из представления.</summary>
    public static CategoryItemResponse From(CategoryItemView item)
        => new(item.Id, item.Name, item.SortOrder, item.IsActive);
}

/// <summary>Раздел с подразделами в административном дереве.</summary>
public sealed record CategorySectionResponse(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<CategoryItemResponse> Subsections)
{
    /// <summary>Создаёт ответ из представления.</summary>
    public static CategorySectionResponse From(CategorySectionView section)
        => new(
            section.Id,
            section.Name,
            section.SortOrder,
            section.IsActive,
            section.Subsections.Select(CategoryItemResponse.From).ToList());
}