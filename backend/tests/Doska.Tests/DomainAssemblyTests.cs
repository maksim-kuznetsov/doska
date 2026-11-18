using Doska.Domain.Entities;

namespace Doska.Tests;

/// <summary>
/// Направление зависимостей: доменный слой не ссылается ни на один проект
/// <c>Doska.*</c> и не зависит от средств доступа к данным.
/// </summary>
public sealed class DomainAssemblyTests
{
    private static string[] ReferencedAssemblyNames()
    {
        return typeof(Entity).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
    }

    [Fact]
    public void Domain_assembly_has_no_project_references_to_other_doska_layers()
    {
        var ownReferences = ReferencedAssemblyNames()
            .Where(name => name.StartsWith("Doska.", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(ownReferences);
    }

    [Fact]
    public void Domain_assembly_does_not_reference_data_access_packages()
    {
        var references = ReferencedAssemblyNames();

        Assert.DoesNotContain(
            references,
            name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(
            references,
            name => name.StartsWith("Npgsql", StringComparison.Ordinal));
    }
}
