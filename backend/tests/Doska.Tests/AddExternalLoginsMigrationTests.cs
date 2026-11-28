using System.Reflection;
using Doska.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Doska.Tests;

/// <summary>
/// Порядок операций миграции переноса Google-привязок: таблица создаётся и заполняется
/// до удаления колонки, иначе данные теряются.
/// </summary>
public sealed class AddExternalLoginsMigrationTests
{
    [Fact]
    public void Up_creates_table_then_backfills_then_drops_column()
    {
        var operations = Operations(forwards: true);

        var createTable = IndexOf<CreateTableOperation>(operations);
        var insert = IndexOf<SqlOperation>(operations, sql => sql.Contains("INSERT INTO external_logins", StringComparison.Ordinal));
        var dropColumn = IndexOf<DropColumnOperation>(operations);

        Assert.True(createTable >= 0, "миграция должна создавать external_logins");
        Assert.True(insert > createTable, "перенос данных должен идти после создания таблицы");
        Assert.True(dropColumn > insert, "колонка google_subject должна удаляться после переноса");

        var backfill = (SqlOperation)operations[insert];
        Assert.Contains("google_subject", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("'google'", backfill.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_readds_column_and_backfills_before_dropping_table()
    {
        var operations = Operations(forwards: false);

        var addColumn = IndexOf<AddColumnOperation>(operations);
        var backfill = IndexOf<SqlOperation>(operations);
        var dropTable = IndexOf<DropTableOperation>(operations);

        Assert.True(addColumn >= 0, "откат должен вернуть колонку google_subject");
        Assert.True(backfill > addColumn, "перенос из external_logins должен идти после возврата колонки");
        Assert.True(dropTable > backfill, "таблица удаляется последней");
    }

    private static IReadOnlyList<MigrationOperation> Operations(bool forwards)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var method = typeof(AddExternalLogins).GetMethod(
            forwards ? "Up" : "Down",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        method.Invoke(new AddExternalLogins(), [builder]);
        return builder.Operations.ToList();
    }

    private static int IndexOf<T>(IReadOnlyList<MigrationOperation> operations, Func<string, bool>? sqlPredicate = null)
        where T : MigrationOperation
    {
        return operations
            .Select((operation, index) => (operation, index))
            .Where(entry => entry.operation is T
                && (sqlPredicate is null || (entry.operation is SqlOperation sql && sqlPredicate(sql.Sql))))
            .Select(entry => entry.index)
            .DefaultIfEmpty(-1)
            .First();
    }
}
