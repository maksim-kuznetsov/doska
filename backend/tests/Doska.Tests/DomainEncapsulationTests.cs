using System.Reflection;
using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>
/// Инкапсуляция состояния: у сущностей домена нет публичных сеттеров,
/// поэтому состояние меняется только доменными методами и конструкторами.
/// </summary>
public sealed class DomainEncapsulationTests
{
    /// <summary>Все конкретные и базовые типы сущностей домена.</summary>
    public static IEnumerable<object[]> EntityTypes()
        => typeof(Entity).Assembly
            .GetTypes()
            .Where(type => type.IsClass && typeof(Entity).IsAssignableFrom(type))
            .Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Entity_state_has_no_public_setter(Type entityType)
    {
        var writable = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();

        Assert.Empty(writable);
    }
}
