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
/// применяется командой
/// <c>dotnet tool run dotnet-ef database update --project src/Doska.Infrastructure --startup-project src/Doska.Api</c>
/// или автоматически при старте API (настройка <c>Database:MigrateOnStartup</c>).
/// </para>
/// <para>
/// Отметки <c>created_at</c> и <c>updated_at</c> принадлежат домену: их проставляют
/// конструкторы и доменные методы сущностей, а не сохранение.
/// </para>
/// </summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    /// <summary>Создаёт контекст.</summary>
    /// <param name="options">Параметры подключения и поведения контекста.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>Пользователи.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Идентичности внешних провайдеров входа.</summary>
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

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
}
