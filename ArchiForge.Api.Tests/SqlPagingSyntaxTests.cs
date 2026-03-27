using ArchiForge.Data.Infrastructure;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class SqlPagingSyntaxTests
{
    [Fact]
    public void FirstRowsOnly_SqliteConnection_UsesLimitSyntax()
    {
        using SqliteConnection connection = new("Data Source=:memory:");

        string clause = SqlPagingSyntax.FirstRowsOnly(connection, 200);

        clause.Should().Be("LIMIT 200");
    }

    [Fact]
    public void FirstRowsOnly_SqlServerConnection_UsesOffsetFetchSyntax()
    {
        using SqlConnection connection = new("Server=.;Database=ArchiForge;TrustServerCertificate=True");

        string clause = SqlPagingSyntax.FirstRowsOnly(connection, 200);

        clause.Should().Be("OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY");
    }
}
