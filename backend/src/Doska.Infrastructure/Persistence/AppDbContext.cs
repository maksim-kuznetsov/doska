using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Persistence;

/// <summary>
/// Контекст доступа к PostgreSQL.
/// <para>
/// Схема базы данных меняется только миграциями EF Core: ручные правки в базе запрещены,
/// потому что они расходятся с моделью и следующей миграцией. Новая миграция создаётся из
/// каталога <c>backend</c> командой
/// <c>dotnet tool run dotnet-ef migrations add ИмяМиграции --project src/Doska.Infrastructure --startup-project src/Doska.Api --output-dir Migrations</c>,
/// применяется командой <c>dotnet tool run dotnet-ef database update --project src/Doska.Infrastructure --startup-project src/Doska.Api</c>
/// или автоматически при старте API (настройка <c>Database:MigrateOnStartup</c>).
/// </para>
/// <para>
/// Отметки <c>created_at</c> и <c>updated_at</c> заполняются здесь при сохранении
/// по <see cref="TimeProvider"/>, а не триггерами базы: время задаётся в одном месте
/// и подменяется в тестах.
/// </para>
/// </summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    private readonly TimeProvider _timeProvider;

    /// <summary>Создаёт контекст.</summary>
    /// <param name="options">Параметры подключения и поведения контекста.</param>
    /// <param name="timeProvider">Источник времени для отметок создания и изменения.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider)
        : base(options)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Пользователи.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Одноразовые токены подтверждения почты и сброса пароля.</summary>
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    /// <summary>Города.</summary>
    public DbSet<City> Cities => Set<City>();

    /// <summary>Разделы и подразделы каталога.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Объявления.</summary>
    public DbSet<Ad> Ads => Set<Ad>();

    /// <summary>Фотографии объявлений.</summary>
    public DbSet<AdPhoto> AdPhotos => Set<AdPhoto>();

    /// <summary>Жалобы на объявления.</summary>
    public DbSet<AdComplaint> AdComplaints => Set<AdComplaint>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
