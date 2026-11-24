using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>Правила жизненного цикла города.</summary>
public sealed class CityDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rename_trims_and_accepts_valid_name()
    {
        var city = new City("Москва", Now);

        city.Rename("  Тверь  ", Now);

        Assert.Equal("Тверь", city.Name);
    }

    [Fact]
    public void Rename_rejects_empty_name()
    {
        var city = new City("Москва", Now);

        Assert.Throws<ArgumentException>(() => city.Rename("   ", Now));
    }

    [Fact]
    public void Rename_rejects_too_long_name()
    {
        var city = new City("Москва", Now);

        Assert.Throws<ArgumentException>(() => city.Rename(new string('а', City.MaxNameLength + 1), Now));
    }

    [Fact]
    public void Deactivate_and_activate_change_availability()
    {
        var city = new City("Москва", Now);

        city.Deactivate(Now);
        Assert.False(city.IsActive);

        city.Activate(Now);
        Assert.True(city.IsActive);
    }
}
