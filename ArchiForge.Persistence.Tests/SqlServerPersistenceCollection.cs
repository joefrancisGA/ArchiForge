namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Groups tests that share one SQL Server container + DbUp-applied schema (see <see cref="SqlServerPersistenceFixture"/>).
/// </summary>
[CollectionDefinition(nameof(SqlServerPersistenceCollection))]
public sealed class SqlServerPersistenceCollection : ICollectionFixture<SqlServerPersistenceFixture>
{
}
