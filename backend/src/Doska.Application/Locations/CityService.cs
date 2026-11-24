using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Locations;

/// <summary>Город в административном списке.</summary>
public sealed record CityView(Guid Id, string Name, bool IsActive);

/// <summary>Управление справочником городов.</summary>
public sealed class CityService(IAppDbContext db, TimeProvider clock)
{
    /// <summary>Полный список городов для администратора, включая неактивные.</summary>
    public async Task<IReadOnlyList<CityView>> GetAdminListAsync(
        CancellationToken cancellationToken = default)
        => await db.Cities
            .OrderBy(city => city.Name)
            .Select(city => new CityView(city.Id, city.Name, city.IsActive))
            .ToListAsync(cancellationToken);

    /// <summary>Добавляет город в справочник.</summary>
    public async Task<CityView> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = await NormalizeAvailableNameAsync(name, null, cancellationToken);
        var city = new City(normalizedName, clock.GetUtcNow());

        db.Cities.Add(city);
        await db.SaveChangesAsync(cancellationToken);

        return new CityView(city.Id, city.Name, city.IsActive);
    }

    /// <summary>Переименовывает город.</summary>
    public async Task<CityView> RenameAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken = default)
    {
        var city = await LoadAsync(id, cancellationToken);
        var normalizedName = await NormalizeAvailableNameAsync(name, id, cancellationToken);

        city.Rename(normalizedName, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return new CityView(city.Id, city.Name, city.IsActive);
    }

    /// <summary>Делает город доступным для выбора.</summary>
    public async Task<CityView> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var city = await LoadAsync(id, cancellationToken);
        city.Activate(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return new CityView(city.Id, city.Name, city.IsActive);
    }

    /// <summary>Скрывает город из выбора, не затрагивая объявления.</summary>
    public async Task<CityView> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var city = await LoadAsync(id, cancellationToken);
        city.Deactivate(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return new CityView(city.Id, city.Name, city.IsActive);
    }

    /// <summary>Удаляет город, если у него нет объявлений.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var city = await LoadAsync(id, cancellationToken);
        var hasAds = await db.Ads.AnyAsync(ad => ad.CityId == id, cancellationToken);

        if (hasAds)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Нельзя удалить город с объявлениями. Деактивируйте его.");
        }

        db.Cities.Remove(city);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<City> LoadAsync(Guid id, CancellationToken cancellationToken)
        => await db.Cities.FirstOrDefaultAsync(city => city.Id == id, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Город не найден.");

    private async Task<string> NormalizeAvailableNameAsync(
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Название города обязательно.");
        }

        if (trimmed.Length > City.MaxNameLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                $"Название города не длиннее {City.MaxNameLength} символов.");
        }

        var query = db.Cities.Where(city => city.Name == trimmed);

        if (exceptId is Guid id)
        {
            query = query.Where(city => city.Id != id);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Город с таким названием уже существует.");
        }

        return trimmed;
    }
}