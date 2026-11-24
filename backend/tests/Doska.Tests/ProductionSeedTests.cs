using Doska.Application.Abstractions;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Infrastructure.Seed;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Коллекция для тестов, сравнивающих общие счётчики в базе.</summary>
[CollectionDefinition("Database", DisableParallelization = true)]
public sealed class DatabaseCollection;

/// <summary>Проверяет команды первичной инициализации production.</summary>
[Collection("Database")]
public sealed class ProductionSeedTests : IClassFixture<ApiFactory>
{
    private const string AdminPassword = "adminpass123";
    private const string NewAdminPassword = "newadminpass123";

    private readonly ApiFactory _factory;

    public ProductionSeedTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Init_data_creates_references_and_is_idempotent()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var before = await CountAsync(ct);
        await SeedReferencesAsync(ct);
        var afterFirst = await CountAsync(ct);
        await SeedReferencesAsync(ct);
        var afterSecond = await CountAsync(ct);

        Assert.Equal(afterFirst, afterSecond);
        Assert.True(afterFirst.Cities >= before.Cities);
        Assert.True(afterFirst.Categories >= before.Categories);
        Assert.Equal(before.Users, afterSecond.Users);

        var section = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Categories.FirstAsync(
                category => category.Name == "Транспорт" && category.ParentId == null,
                ct));
        var subsection = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Categories.FirstOrDefaultAsync(
                category => category.Name == "Автомобили" && category.ParentId == section.Id,
                ct));

        Assert.NotNull(subsection);
    }

    [Fact]
    public async Task Create_admin_creates_promotes_and_is_idempotent()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = $"production-admin-{Guid.NewGuid():N}@example.com";

        try
        {
            var created = await CreateAdminAsync(email, AdminPassword, ct);
            Assert.True(created);

            var admin = await FindUserAsync(email, ct);
            Assert.Equal(UserRole.Admin, admin.Role);
            Assert.True(admin.IsEmailConfirmed);

            var hasher = await GetHasherAsync();
            Assert.True(hasher.Verify(AdminPassword, admin.PasswordHash));

            var createdAgain = await CreateAdminAsync(email, NewAdminPassword, ct);
            Assert.False(createdAgain);

            var users = await TestDatabase.WithContextAsync(
                _factory,
                db => db.Users.CountAsync(user => user.Email == email, ct));
            Assert.Equal(1, users);

            var updated = await FindUserAsync(email, ct);
            Assert.True(hasher.Verify(NewAdminPassword, updated.PasswordHash));
            Assert.False(hasher.Verify(AdminPassword, updated.PasswordHash));
        }
        finally
        {
            await TestDatabase.WithContextAsync(_factory, async db =>
            {
                await db.Users.Where(user => user.Email == email).ExecuteDeleteAsync(ct);
            });
        }
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private Task SeedReferencesAsync(CancellationToken cancellationToken)
    {
        return TestDatabase.WithContextAsync(
            _factory,
            db => ReferenceDataSeeder.SeedAsync(db, TimeProvider.System, cancellationToken));
    }

    private async Task<bool> CreateAdminAsync(string email, string password, CancellationToken cancellationToken)
    {
        return await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var hasher = provider.GetRequiredService<IPasswordHasher>();
            return await AdminSeeder.CreateOrUpdateAsync(
                db,
                hasher,
                email,
                password,
                TimeProvider.System,
                cancellationToken);
        });
    }

    private Task<Doska.Domain.Entities.User> FindUserAsync(string email, CancellationToken cancellationToken)
    {
        return TestDatabase.WithContextAsync(
            _factory,
            db => db.Users.FirstAsync(user => user.Email == email, cancellationToken));
    }

    private Task<IPasswordHasher> GetHasherAsync()
    {
        return TestDatabase.WithServicesAsync(
            _factory,
            provider => Task.FromResult(provider.GetRequiredService<IPasswordHasher>()));
    }

    private Task<(int Cities, int Categories, int Users)> CountAsync(CancellationToken cancellationToken)
    {
        return TestDatabase.WithContextAsync(
            _factory,
            async db => (
                await db.Cities.CountAsync(cancellationToken),
                await db.Categories.CountAsync(cancellationToken),
                await db.Users.CountAsync(cancellationToken)));
    }
}