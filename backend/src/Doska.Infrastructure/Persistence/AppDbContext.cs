using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Persistence;

/// <summary>
/// Контекст доступа к PostgreSQL. Пока пуст: сущности и миграции появятся
/// в отдельных изменениях, чтобы каркас не фиксировал модель данных.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
