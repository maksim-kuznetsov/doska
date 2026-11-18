namespace Doska.Application.Common;

/// <summary>Страница результатов с общим количеством записей до пагинации.</summary>
/// <typeparam name="T">Тип элемента.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);