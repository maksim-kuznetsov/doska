using Doska.Domain.Entities;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Doska.Tests;

/// <summary>
/// Проверка конфигураций EF Core: семь таблиц, ограничения целостности,
/// типы столбцов, индексы и поведение удаления связей.
/// </summary>
public sealed class ModelConfigurationTests : IDisposable
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=doska;Username=doska;Password=doska";

    private readonly AppDbContext _context;

    public ModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new AppDbContext(options);
    }

    /// <summary>
    /// Полная модель времени разработки: оптимизированная для выполнения модель
    /// (<c>DbContext.Model</c>) не хранит ограничения <c>CHECK</c>.
    /// </summary>
    private IReadOnlyModel Model => _context.GetService<IDesignTimeModel>().Model;

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();

    [Fact]
    public void Model_contains_seven_entity_types()
    {
        var entityTypes = Model.GetEntityTypes().Select(entityType => entityType.ClrType).ToArray();

        Assert.Equal(7, entityTypes.Length);
        Assert.Contains(typeof(User), entityTypes);
        Assert.Contains(typeof(UserToken), entityTypes);
        Assert.Contains(typeof(City), entityTypes);
        Assert.Contains(typeof(Category), entityTypes);
        Assert.Contains(typeof(Ad), entityTypes);
        Assert.Contains(typeof(AdPhoto), entityTypes);
        Assert.Contains(typeof(AdComplaint), entityTypes);
    }

    [Theory]
    [InlineData(typeof(User), "users")]
    [InlineData(typeof(UserToken), "user_tokens")]
    [InlineData(typeof(City), "cities")]
    [InlineData(typeof(Category), "categories")]
    [InlineData(typeof(Ad), "ads")]
    [InlineData(typeof(AdPhoto), "ad_photos")]
    [InlineData(typeof(AdComplaint), "ad_complaints")]
    public void Entity_is_mapped_to_expected_table(Type clrType, string tableName)
    {
        Assert.Equal(tableName, FindEntityType(clrType).GetTableName());
    }

    [Theory]
    [InlineData(typeof(User), "ck_users_role")]
    [InlineData(typeof(UserToken), "ck_user_tokens_purpose")]
    [InlineData(typeof(Category), "ck_categories_sort_order")]
    [InlineData(typeof(Ad), "ck_ads_status")]
    [InlineData(typeof(Ad), "ck_ads_price_non_negative")]
    [InlineData(typeof(AdPhoto), "ck_ad_photos_sort_order")]
    [InlineData(typeof(AdComplaint), "ck_ad_complaints_status")]
    public void Check_constraint_is_defined(Type clrType, string constraintName)
    {
        var entityType = FindEntityType(clrType);

        Assert.Contains(
            entityType.GetCheckConstraints(),
            constraint => constraint.Name == constraintName);
    }

    [Theory]
    [InlineData(typeof(User), nameof(User.Email), "character varying(320)")]
    [InlineData(typeof(User), nameof(User.GoogleSubject), "character varying(255)")]
    [InlineData(typeof(User), nameof(User.HasPassword), "boolean")]
    [InlineData(typeof(User), nameof(User.Role), "character varying(20)")]
    [InlineData(typeof(UserToken), nameof(UserToken.TokenHash), "character varying(64)")]
    [InlineData(typeof(Ad), nameof(Ad.Title), "character varying(200)")]
    [InlineData(typeof(Ad), nameof(Ad.Description), "text")]
    [InlineData(typeof(Ad), nameof(Ad.Price), "numeric(12,2)")]
    [InlineData(typeof(Ad), nameof(Ad.PublishedAt), "timestamp with time zone")]
    [InlineData(typeof(Ad), nameof(Ad.ModerationReason), "text")]
    [InlineData(typeof(AdComplaint), nameof(AdComplaint.Reason), "text")]
    [InlineData(typeof(AdComplaint), nameof(AdComplaint.Status), "character varying(20)")]
    public void Column_type_matches_conventions(Type clrType, string propertyName, string columnType)
    {
        var property = FindEntityType(clrType).FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(columnType, property!.GetColumnType());
    }

    [Theory]
    [InlineData(typeof(User), nameof(User.Role))]
    [InlineData(typeof(UserToken), nameof(UserToken.Purpose))]
    [InlineData(typeof(Ad), nameof(Ad.Status))]
    [InlineData(typeof(AdComplaint), nameof(AdComplaint.Status))]
    public void Enum_is_stored_as_string(Type clrType, string propertyName)
    {
        var property = FindEntityType(clrType).FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.True(property!.ClrType.IsEnum);

        var converter = property.GetTypeMapping().Converter;

        Assert.NotNull(converter);
        Assert.Equal(typeof(string), converter!.ProviderClrType);
        Assert.StartsWith("character varying", property.GetColumnType(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(User), "Email", true)]
    [InlineData(typeof(User), "GoogleSubject", true)]
    [InlineData(typeof(City), "Name", true)]
    [InlineData(typeof(Category), "Name", true)]
    [InlineData(typeof(UserToken), "TokenHash", true)]
    [InlineData(typeof(UserToken), "UserId,Purpose", false)]
    [InlineData(typeof(Ad), "Status,PublishedAt", false)]
    [InlineData(typeof(Ad), "CategoryId,Status", false)]
    [InlineData(typeof(Ad), "CityId", false)]
    [InlineData(typeof(Ad), "AuthorId,CreatedAt", false)]
    [InlineData(typeof(AdPhoto), "AdId,SortOrder", true)]
    [InlineData(typeof(AdComplaint), "AdId,AuthorId", true)]
    [InlineData(typeof(AdComplaint), "Status,CreatedAt", false)]
    public void Index_is_defined(Type clrType, string propertyNames, bool isUnique)
    {
        var expectedProperties = propertyNames.Split(',');
        var entityType = FindEntityType(clrType);

        var index = entityType
            .GetIndexes()
            .SingleOrDefault(candidate => candidate
                .Properties
                .Select(property => property.Name)
                .SequenceEqual(expectedProperties));

        Assert.NotNull(index);
        Assert.Equal(isUnique, index!.IsUnique);
    }

    [Fact]
    public void HasPassword_has_true_default_for_existing_users()
    {
        var property = FindEntityType(typeof(User)).FindProperty(nameof(User.HasPassword));

        Assert.NotNull(property);
        Assert.False(property!.IsNullable);
        Assert.Equal(true, property.GetDefaultValue());
    }

    [Theory]
    [InlineData(typeof(Ad), "AuthorId", typeof(User), DeleteBehavior.Restrict)]
    [InlineData(typeof(Ad), "CategoryId", typeof(Category), DeleteBehavior.Restrict)]
    [InlineData(typeof(Ad), "CityId", typeof(City), DeleteBehavior.Restrict)]
    [InlineData(typeof(AdPhoto), "AdId", typeof(Ad), DeleteBehavior.Cascade)]
    [InlineData(typeof(AdComplaint), "AdId", typeof(Ad), DeleteBehavior.Cascade)]
    [InlineData(typeof(AdComplaint), "AuthorId", typeof(User), DeleteBehavior.Restrict)]
    [InlineData(typeof(AdComplaint), "ClosedByUserId", typeof(User), DeleteBehavior.Restrict)]
    [InlineData(typeof(UserToken), "UserId", typeof(User), DeleteBehavior.Cascade)]
    [InlineData(typeof(Category), "ParentId", typeof(Category), DeleteBehavior.Restrict)]
    public void Foreign_key_delete_behavior_is_configured(
        Type dependentClrType,
        string foreignKeyProperty,
        Type principalClrType,
        DeleteBehavior deleteBehavior)
    {
        var entityType = FindEntityType(dependentClrType);

        var foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(candidate =>
                candidate.PrincipalEntityType.ClrType == principalClrType &&
                candidate.Properties.Any(property => property.Name == foreignKeyProperty));

        Assert.NotNull(foreignKey);
        Assert.Equal(deleteBehavior, foreignKey!.DeleteBehavior);
    }

    private IReadOnlyEntityType FindEntityType(Type clrType)
    {
        var entityType = Model.FindEntityType(clrType);

        Assert.NotNull(entityType);
        return entityType!;
    }
}
