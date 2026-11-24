using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>
/// Правила каталога: глубина ограничена двумя уровнями, поэтому подраздел
/// не может быть родителем, а категория не может быть родителем самой себе.
/// </summary>
public sealed class CategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Subsection_can_be_attached_to_section()
    {
        var section = new Category("Транспорт", 1, Now);
        var subsection = new Category("Автомобили", 1, Now);

        subsection.SetParent(section, Now);

        Assert.True(section.IsSection);
        Assert.False(subsection.IsSection);
        Assert.Equal(section.Id, subsection.ParentId);
        Assert.Same(section, subsection.Parent);
    }

    [Fact]
    public void Third_level_is_rejected()
    {
        var section = new Category("Транспорт", 1, Now);
        var subsection = new Category("Автомобили", 1, Now);
        subsection.SetParent(section, Now);
        var tooDeep = new Category("Запчасти", 1, Now);

        var exception = Assert.Throws<InvalidOperationException>(() => tooDeep.SetParent(subsection, Now));

        Assert.Contains("двумя уровнями", exception.Message, StringComparison.Ordinal);
        Assert.Null(tooDeep.ParentId);
        Assert.Null(tooDeep.Parent);
    }

    [Fact]
    public void Category_cannot_be_its_own_parent()
    {
        var section = new Category("Транспорт", 1, Now);

        var exception = Assert.Throws<InvalidOperationException>(() => section.SetParent(section, Now));

        Assert.Contains("самой себя", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Clearing_parent_turns_subsection_back_into_section()
    {
        var section = new Category("Транспорт", 1, Now);
        var subsection = new Category("Автомобили", 1, Now);
        subsection.SetParent(section, Now);

        subsection.SetParent(null, Now);

        Assert.True(subsection.IsSection);
        Assert.Null(subsection.ParentId);
        Assert.Null(subsection.Parent);
    }
}
