using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>Правила создания и изменения категорий.</summary>
public sealed class CategoryManagementDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_trims_name_and_sets_order()
    {
        var section = new Category("  Транспорт  ", 3, Now);

        Assert.Equal("Транспорт", section.Name);
        Assert.Equal(3, section.SortOrder);
        Assert.True(section.IsSection);
        Assert.True(section.IsActive);
    }

    [Fact]
    public void Subsection_constructor_attaches_to_section()
    {
        var section = new Category("Транспорт", 1, Now);

        var subsection = new Category("Автомобили", 2, section, Now);

        Assert.False(subsection.IsSection);
        Assert.Equal(section.Id, subsection.ParentId);
        Assert.Equal(2, subsection.SortOrder);
    }

    [Fact]
    public void Subsection_constructor_rejects_third_level()
    {
        var section = new Category("Транспорт", 1, Now);
        var subsection = new Category("Автомобили", 0, section, Now);

        Assert.Throws<InvalidOperationException>(() =>
            new Category("Запчасти", 0, subsection, Now));
    }

    [Fact]
    public void Rename_rejects_empty_and_too_long_names()
    {
        var section = new Category("Транспорт", 1, Now);

        Assert.Throws<ArgumentException>(() => section.Rename("   ", Now));
        Assert.Throws<ArgumentException>(() => section.Rename(new string('а', Category.MaxNameLength + 1), Now));
    }

    [Fact]
    public void SetSortOrder_rejects_negative_values()
    {
        var section = new Category("Транспорт", 1, Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => section.SetSortOrder(-1, Now));
        Assert.Equal(1, section.SortOrder);
    }

    [Fact]
    public void Deactivate_and_activate_toggle_availability()
    {
        var section = new Category("Транспорт", 1, Now);

        section.Deactivate(Now);
        Assert.False(section.IsActive);

        section.Activate(Now);
        Assert.True(section.IsActive);
    }

    [Fact]
    public void Section_with_children_cannot_become_subsection()
    {
        var section = new Category("Транспорт", 1, Now);
        var subsection = new Category("Автомобили", 0, section, Now);
        section.Children.Add(subsection);

        var anotherSection = new Category("Недвижимость", 2, Now);

        Assert.Throws<InvalidOperationException>(() => section.SetParent(anotherSection, Now));
    }
}
