using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>Правила жизненного цикла города.</summary>
public sealed class CityDomainTests
{
    [Fact]
    public void Rename_trims_and_accepts_valid_name()
    {
        var city = new City { Name = "Москва" };

        city.Rename("  Тверь  ");

        Assert.Equal("Тверь", city.Name);
    }

    [Fact]
    public void Rename_rejects_empty_name()
    {
        var city = new City { Name = "Москва" };

        Assert.Throws<ArgumentException>(() => city.Rename("   "));
    }

    [Fact]
    public void Rename_rejects_too_long_name()
    {
        var city = new City { Name = "Москва" };

        Assert.Throws<ArgumentException>(() => city.Rename(new string('а', City.MaxNameLength + 1)));
    }

    [Fact]
    public void Deactivate_and_activate_change_availability()
    {
        var city = new City { Name = "Москва" };

        city.Deactivate();
        Assert.False(city.IsActive);

        city.Activate();
        Assert.True(city.IsActive);
    }
}