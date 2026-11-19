using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Media;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Doska.Tests;

/// <summary>Идемпотентная очистка осиротевших файлов медиа.</summary>
public sealed class MediaMaintenanceTests : IClassFixture<ApiFactory>
{
    private const string KeepFile = "keep.png";
    private const string KeepThumbnail = "keep_thumb.jpg";

    private readonly ApiFactory _factory;

    public MediaMaintenanceTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Cleanup_removes_orphan_directories_and_unreferenced_files()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, cancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }

        var root = Path.Combine(
            AppContext.BaseDirectory,
            "media-cleanup-tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            await TestDatabase.WithServicesAsync(_factory, async provider =>
            {
                var db = provider.GetRequiredService<AppDbContext>();
                var fixture = await CreateFixtureAsync(db, cancellationToken);

                try
                {
                    var photosRoot = Path.Combine(root, "photos");
                    var adDirectory = Path.Combine(photosRoot, fixture.AdId.ToString());
                    Directory.CreateDirectory(adDirectory);

                    await File.WriteAllTextAsync(
                        Path.Combine(adDirectory, KeepFile),
                        "keep",
                        cancellationToken);
                    await File.WriteAllTextAsync(
                        Path.Combine(adDirectory, KeepThumbnail),
                        "keep",
                        cancellationToken);
                    await File.WriteAllTextAsync(
                        Path.Combine(adDirectory, "orphan.png"),
                        "orphan",
                        cancellationToken);

                    var unknownAdDirectory = Path.Combine(
                        photosRoot,
                        Guid.NewGuid().ToString());
                    Directory.CreateDirectory(unknownAdDirectory);
                    await File.WriteAllTextAsync(
                        Path.Combine(unknownAdDirectory, "file.png"),
                        "orphan",
                        cancellationToken);

                    var invalidDirectory = Path.Combine(photosRoot, "not-a-guid");
                    Directory.CreateDirectory(invalidDirectory);
                    await File.WriteAllTextAsync(
                        Path.Combine(invalidDirectory, "file.png"),
                        "orphan",
                        cancellationToken);

                    var service = new MediaMaintenanceService(
                        db,
                        new StubHostEnvironment(AppContext.BaseDirectory),
                        Options.Create(new MediaOptions { RootPath = root }));

                    var result = await service.CleanupAsync(cancellationToken);

                    Assert.Equal(2, result.RemovedDirectories);
                    Assert.Equal(1, result.RemovedFiles);
                    Assert.True(File.Exists(Path.Combine(adDirectory, KeepFile)));
                    Assert.True(File.Exists(Path.Combine(adDirectory, KeepThumbnail)));
                    Assert.False(File.Exists(Path.Combine(adDirectory, "orphan.png")));
                    Assert.False(Directory.Exists(unknownAdDirectory));
                    Assert.False(Directory.Exists(invalidDirectory));

                    var repeated = await service.CleanupAsync(cancellationToken);
                    Assert.Equal(new MediaCleanupResult(0, 0), repeated);

                    return 0;
                }
                finally
                {
                    await DeleteFixtureAsync(db, fixture, cancellationToken);
                }
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static async Task<Fixture> CreateFixtureAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var author = new User
        {
            Email = $"media-cleanup-{suffix}@example.com",
            PasswordHash = "test",
            Role = UserRole.User,
            IsEmailConfirmed = true,
        };
        var section = new Category { Name = $"Media cleanup section {suffix}", SortOrder = 1 };
        var city = new City { Name = $"Media cleanup city {suffix}" };

        db.Users.Add(author);
        db.Categories.Add(section);
        db.Cities.Add(city);
        await db.SaveChangesAsync(cancellationToken);

        var subsection = new Category
        {
            Name = $"Media cleanup subsection {suffix}",
            ParentId = section.Id,
            SortOrder = 0,
        };
        db.Categories.Add(subsection);
        await db.SaveChangesAsync(cancellationToken);

        var ad = new Ad
        {
            AuthorId = author.Id,
            CategoryId = subsection.Id,
            CityId = city.Id,
            Title = $"Media cleanup ad {suffix}",
            Description = "Описание",
            Price = 1m,
            Contacts = "+7 900 000-00-00",
            Status = AdStatus.Active,
            PublishedAt = DateTimeOffset.UtcNow,
        };
        ad.Photos.Add(new AdPhoto
        {
            AdId = ad.Id,
            FileName = KeepFile,
            ThumbnailFileName = KeepThumbnail,
            ContentType = "image/png",
            ByteSize = 4,
            SortOrder = 1,
            Width = 1,
            Height = 1,
        });

        db.Ads.Add(ad);
        await db.SaveChangesAsync(cancellationToken);

        return new Fixture(ad.Id, author.Id, section.Id, subsection.Id, city.Id);
    }

    private static async Task DeleteFixtureAsync(
        AppDbContext db,
        Fixture fixture,
        CancellationToken cancellationToken)
    {
        db.Ads.RemoveRange(db.Ads.Where(ad => ad.Id == fixture.AdId));
        await db.SaveChangesAsync(cancellationToken);

        db.Categories.RemoveRange(db.Categories.Where(category => category.Id == fixture.SubsectionId));
        await db.SaveChangesAsync(cancellationToken);

        db.Categories.RemoveRange(db.Categories.Where(category => category.Id == fixture.SectionId));
        db.Cities.RemoveRange(db.Cities.Where(city => city.Id == fixture.CityId));
        db.Users.RemoveRange(db.Users.Where(user => user.Id == fixture.AuthorId));
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record Fixture(
        Guid AdId,
        Guid AuthorId,
        Guid SectionId,
        Guid SubsectionId,
        Guid CityId);

    private sealed class StubHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Doska.Tests";

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}