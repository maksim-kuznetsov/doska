using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Doska.Tests;

/// <summary>
/// Согласованность перечислений и ограничений <c>CHECK</c>: значения из
/// <c>Doska.Domain</c> и список значений в ограничении не должны расходиться.
/// </summary>
public sealed class EnumConstraintTests : IDisposable
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=doska;Username=doska;Password=doska";

    private readonly AppDbContext _context;

    public EnumConstraintTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new AppDbContext(options, TimeProvider.System);
    }

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(typeof(User), "ck_users_role", typeof(UserRole))]
    [InlineData(typeof(UserToken), "ck_user_tokens_purpose", typeof(TokenPurpose))]
    [InlineData(typeof(Ad), "ck_ads_status", typeof(AdStatus))]
    [InlineData(typeof(AdComplaint), "ck_ad_complaints_status", typeof(ComplaintStatus))]
    public void Check_constraint_lists_every_enum_member(Type clrType, string constraintName, Type enumType)
    {
        IReadOnlyEntityType entityType = _context
            .GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(clrType)!;

        var constraint = Assert.Single(
            entityType.GetCheckConstraints(),
            candidate => candidate.Name == constraintName);

        foreach (var memberName in Enum.GetNames(enumType))
        {
            Assert.Contains($"'{memberName}'", constraint.Sql!, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(typeof(UserRole), 3)]
    [InlineData(typeof(AdStatus), 6)]
    [InlineData(typeof(ComplaintStatus), 2)]
    [InlineData(typeof(TokenPurpose), 2)]
    public void Enum_has_expected_number_of_members(Type enumType, int expectedCount)
    {
        Assert.Equal(expectedCount, Enum.GetNames(enumType).Length);
    }
}
