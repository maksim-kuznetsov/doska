using Doska.Application.Locations;

namespace Doska.Api.Contracts;

/// <summary>Запрос добавления или переименования города.</summary>
public sealed record CityRequest(string Name);

/// <summary>Город в административном списке.</summary>
public sealed record CityResponse(Guid Id, string Name, bool IsActive)
{
    /// <summary>Создаёт ответ из представления.</summary>
    public static CityResponse From(CityView city)
        => new(city.Id, city.Name, city.IsActive);
}