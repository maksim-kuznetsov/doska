using Doska.Application.Abstractions;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Infrastructure.Seed;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Проверяет идемпотентность seed демо-данных.</summary>
public sealed class SeedDemoUsersTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SeedDemoUsersTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_creates_demo_users_and_is_idempotent()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, ct))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            await DemoDataSeeder.SeedAsync(db, passwordHasher, ct);
            await DemoDataSeeder.SeedAsync(db, passwordHasher, ct);

            return 0;
        });

        var users = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Users
                .Where(user => user.Email == "user@example.com"
                    || user.Email == "moderator@example.com"
                    || user.Email == "admin@example.com")
                .Select(user => new { user.Email, user.Role })
                .ToListAsync(ct));

        Assert.Equal(3, users.Count);
        Assert.Contains(users, user => user.Email == "user@example.com" && user.Role == UserRole.User);
        Assert.Contains(users, user => user.Email == "moderator@example.com" && user.Role == UserRole.Moderator);
        Assert.Contains(users, user => user.Email == "admin@example.com" && user.Role == UserRole.Admin);

        var hasher = await TestDatabase.WithServicesAsync(
            _factory,
            provider => Task.FromResult(provider.GetRequiredService<IPasswordHasher>()));

        Assert.All(users, user => Assert.NotNull(user));

        var demoUser = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Users.FirstAsync(candidate => candidate.Email == "user@example.com", ct));

        Assert.True(hasher.Verify(DemoDataSeeder.DemoPassword, demoUser.PasswordHash));
        Assert.True(demoUser.IsEmailConfirmed);
    }
}
