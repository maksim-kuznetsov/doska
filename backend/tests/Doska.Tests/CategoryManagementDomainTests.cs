using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>Правила создания и изменения категорий.</summary>
public sealed class CategoryManagementDomainTests
{
    [Fact]
    public void CreateSection_trims_name_and_sets_order()
    {
        var section = Category.CreateSection("  Транспорт  ", 3);

        Assert.Equal("Транспорт", section.Name);
        Assert.Equal(3, section.SortOrder);
        Assert.True(section.IsSection);
        Assert.True(section.IsActive);
    }

    [Fact]
    public void CreateSubsection_attaches_to_section()
    {
        var section = Category.CreateSection("Транспорт", 1);

        var subsection = Category.CreateSubsection(section, "Автомобили", 2);

        Assert.False(subsection.IsSection);
        Assert.Equal(section.Id, subsection.ParentId);
        Assert.Equal(2, subsection.SortOrder);
    }

    [Fact]
    public void CreateSubsection_rejects_third_level()
    {
        var section = Category.CreateSection("Транспорт", 1);
        var subsection = Category.CreateSubsection(section, "Автомобили");

        Assert.Throws<InvalidOperationException>(() =>
            Category.CreateSubsection(subsection, "Запчасти"));
    }

    [Fact]
    public void Rename_rejects_empty_and_too_long_names()
    {
        var section = Category.CreateSection("Транспорт", 1);

        Assert.Throws<ArgumentException>(() => section.Rename("   "));
        Assert.Throws<ArgumentException>(() => section.Rename(new string('а', Category.MaxNameLength + 1)));
    }

    [Fact]
    public void SetSortOrder_rejects_negative_values()
    {
        var section = Category.CreateSection("Транспорт", 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => section.SetSortOrder(-1));
        Assert.Equal(1, section.SortOrder);
    }

    [Fact]
    public void Deactivate_and_activate_toggle_availability()
    {
        var section = Category.CreateSection("Транспорт", 1);

        section.Deactivate();
        Assert.False(section.IsActive);

        section.Activate();
        Assert.True(section.IsActive);
    }

    [Fact]
    public void Section_with_children_cannot_become_subsection()
    {
        var section = Category.CreateSection("Транспорт", 1);
        var subsection = Category.CreateSubsection(section, "Автомобили");
        section.Children.Add(subsection);

        var anotherSection = Category.CreateSection("Недвижимость", 2);

        Assert.Throws<InvalidOperationException>(() => section.SetParent(anotherSection));
    }
}