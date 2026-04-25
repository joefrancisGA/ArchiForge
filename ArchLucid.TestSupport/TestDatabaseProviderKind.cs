namespace ArchLucid.TestSupport;

/// <summary>
///     Test-time database backend selection. Dapper and API integration tests use SQL Server only;
///     there is no SQLite provider in this solution.
/// </summary>
public enum TestDatabaseProviderKind
{
    /// <summary>Microsoft SQL Server (LocalDB, container, or full instance). Default for all DB-facing tests.</summary>
    SqlServer = 0
}
