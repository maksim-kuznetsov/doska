using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Abstractions;

/// <summary>
/// Абстракция доступа к данным для сценариев приложения. Реализация —
/// <c>Doska.Infrastructure.Persistence.AppDbContext</c>.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Пользователи.</summary>
    DbSet<User> Users { get; }

    /// <summary>Идентичности внешних провайдеров входа.</summary>
    DbSet<ExternalLogin> ExternalLogins { get; }

    /// <summary>Одноразовые токены пользователей.</summary>
    DbSet<UserToken> UserTokens { get; }

    /// <summary>Города.</summary>
    DbSet<City> Cities { get; }

    /// <summary>Разделы и подразделы категорий.</summary>
    DbSet<Category> Categories { get; }

    /// <summary>Объявления.</summary>
    DbSet<Ad> Ads { get; }

    /// <summary>Фотографии объявлений.</summary>
    DbSet<AdPhoto> AdPhotos { get; }

    /// <summary>Жалобы на объявления.</summary>
    DbSet<AdComplaint> AdComplaints { get; }

    /// <summary>Сохраняет изменения.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
