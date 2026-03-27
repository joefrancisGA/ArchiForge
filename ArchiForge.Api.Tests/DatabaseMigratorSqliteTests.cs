using ArchiForge.Data.Infrastructure;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class DatabaseMigratorSqliteTests
{
    [Fact]
    public void IsSqliteConnection_ReturnsTrue_ForSharedInMemoryTestConnectionString()
    {
        DatabaseMigrator.IsSqliteConnection(ArchiForgeApiFactory.SqliteInMemoryConnectionString).Should().BeTrue();
    }

    [Fact]
    public void IsSqliteConnection_ReturnsTrue_ForFileBackedDbExtension()
    {
        DatabaseMigrator.IsSqliteConnection("Data Source=./local-dev.sqlite").Should().BeTrue();
    }

    [Fact]
    public void IsSqliteConnection_ReturnsFalse_ForTypicalSqlServerConnectionString()
    {
        DatabaseMigrator
            .IsSqliteConnection(
                "Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=x;TrustServerCertificate=True;")
            .Should()
            .BeFalse();
    }
}
