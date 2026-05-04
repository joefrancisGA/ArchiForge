using System.Data;

using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Sql;
using ArchLucid.Persistence.Tests.Support;
using ArchLucid.TestSupport;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Resolves a SQL Server connection (environment variable or Windows LocalDB), ensures the test catalog exists,
///     applies embedded <see cref="DatabaseMigrator" /> scripts (core Data-layer tables) and a minimal SQL supplement
///     for persistence-only tables (AuditEvents, ConversationThreads, ProvenanceSnapshots) that DbUp omits.
/// </summary>
/// <remarks>
///     No Docker/Testcontainers dependency in this fixture. When <see cref="EnvironmentConnectionStringVariable" /> is
///     unset and LocalDB
///     is unavailable, <see cref="IsSqlServerAvailable" /> is false and SQL integration tests should skip
///     (see <see cref="Xunit.Skip" />.<c>IfNot</c> with <c>[SkippableFact]</c> from Xunit.SkippableFact). You can still filter with
///     <c>dotnet test --filter "Category!=SqlServerContainer"</c>.
/// </remarks>
public sealed class SqlServerPersistenceFixture : IAsyncLifetime
{
    /// <summary>Database name used when the connection string omits Initial Catalog.</summary>
    public const string DefaultTestDatabaseName = "ArchLucidPersistenceTests";

    /// <summary>Full SQL Server connection string; when set, this is the only source tried and failures fail the fixture.</summary>
    public const string EnvironmentConnectionStringVariable = TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable;

    /// <summary>
    ///     <c>sp_getapplock</c> resource for <see cref="MergeGovernanceContractTenantAsync" />: serializes merge+FK child inserts
    ///     across parallel jobs sharing <see cref="DefaultTestDatabaseName" />.
    /// </summary>
    public const string GovernanceContractTenantMergeAppLockResource = "ArchLucid.Persistence.Tests.GovernanceContractTenantMerge";

    /// <summary>
    ///     Message passed to <see cref="Xunit.Skip" />.<c>IfNot</c> when no SQL Server could be reached without an explicit env
    ///     connection string.
    /// </summary>
    public const string SqlServerUnavailableSkipReason =
        "No SQL Server for persistence tests (install LocalDB on Windows or set " + EnvironmentConnectionStringVariable
        + "). Or run: dotnet test --filter \"Category!=SqlServerContainer\".";

    /// <summary>True after a successful connection and schema migration.</summary>
    public bool IsSqlServerAvailable
    {
        get;
        private set;
    }

    /// <summary>Connection string after <see cref="InitializeAsync" /> when <see cref="IsSqlServerAvailable" /> is true.</summary>
    public string ConnectionString
    {
        get;
        private set;
    } = string.Empty;

    public async Task InitializeAsync()
    {
        string? fromEnv = Environment.GetEnvironmentVariable(EnvironmentConnectionStringVariable);

        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            await InitializeFromExplicitConnectionStringOrThrowAsync(
                SqlServerIntegrationTestConnections.NormalizePersistenceConnectionString(fromEnv.Trim(),
                    DefaultTestDatabaseName));

            return;
        }

        if (await TryInitializeFromLocalDbAsync())
            return;

        IsSqlServerAvailable = false;
        ConnectionString = string.Empty;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task InitializeFromExplicitConnectionStringOrThrowAsync(string connectionString)
    {
        try
        {
            await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(connectionString, CancellationToken.None);

            DatabaseMigrator.Run(connectionString);

            await RunPersistenceContractSupplementAsync(connectionString);

            await PrimeGovernanceContractTenantAsync(connectionString);

            ConnectionString = connectionString;
            IsSqlServerAvailable = true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "SQL persistence tests require a reachable server when " + EnvironmentConnectionStringVariable
                                                                         + " is set. See inner exception.",
                ex);
        }
    }

    private async Task<bool> TryInitializeFromLocalDbAsync()
    {
        try
        {
            SqlConnectionStringBuilder localDb = new()
            {
                DataSource = "(localdb)\\mssqllocaldb",
                InitialCatalog = DefaultTestDatabaseName,
                IntegratedSecurity = true,
                TrustServerCertificate = true
            };

            string connectionString = localDb.ConnectionString;

            await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(connectionString, CancellationToken.None);

            DatabaseMigrator.Run(connectionString);

            await RunPersistenceContractSupplementAsync(connectionString);

            await PrimeGovernanceContractTenantAsync(connectionString);

            ConnectionString = connectionString;
            IsSqlServerAvailable = true;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     Applies <c>PersistenceContractSupplement.sql</c>: tables DbUp does not create, without the
    ///     FK-hardening blocks from full <c>ArchLucid.sql</c> (those require seeded authority-chain rows).
    /// </summary>
    private static async Task RunPersistenceContractSupplementAsync(string connectionString)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(SqlServerPersistenceFixture).Assembly.Location)!;
        string scriptPath = Path.Combine(assemblyDir, "Scripts", "PersistenceContractSupplement.sql");

        if (!File.Exists(scriptPath))

            throw new FileNotFoundException(
                "PersistenceContractSupplement.sql not found. Ensure the test project copies Scripts to the output directory.",
                scriptPath);


        SqlSchemaBootstrapper bootstrapper = new(
            new TestSqlConnectionFactory(connectionString),
            scriptPath);

        await bootstrapper.EnsureSchemaAsync(CancellationToken.None);
    }

    /// <summary>
    ///     MERGEs <see cref="GovernanceRepositoryContractScope.TenantId" /> into <c>dbo.Tenants</c> within the caller's transaction
    ///     so a subsequent <c>GovernanceApprovalRequests</c> INSERT in the same transaction satisfies
    ///     <c>FK_GovernanceApprovalRequests_Tenants</c>.
    /// </summary>
    public static async Task MergeGovernanceContractTenantAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        /* Pooled sessions can inherit stale SESSION_CONTEXT; governance tables use RLS block predicates and FK checks
         * must see dbo.Tenants rows reliably in the same transaction as MERGE/INSERT. */

        if (connection is SqlConnection sqlConnection)
            await PersistenceIntegrationTestRlsSession.ApplyArchLucidRlsBypassAndTenantScopeAsync(
                sqlConnection,
                cancellationToken,
                GovernanceRepositoryContractScope.TenantId,
                GovernanceRepositoryContractScope.WorkspaceId,
                GovernanceRepositoryContractScope.ProjectId,
                ambientTransaction: (SqlTransaction)transaction);

        await AcquireGovernanceContractTenantMergeLockAsync(connection, transaction, cancellationToken);

        Guid tenantId = GovernanceRepositoryContractScope.TenantId;
        string slug = "archgov-contract-" + tenantId.ToString("N");

        const string mergeSql = """
                                MERGE INTO dbo.Tenants WITH (HOLDLOCK) AS t
                                USING (SELECT @Id AS Id) AS src ON t.Id = src.Id
                                WHEN MATCHED THEN
                                    UPDATE SET
                                        Name = @Name,
                                        Slug = @Slug,
                                        Tier = @Tier
                                WHEN NOT MATCHED BY TARGET THEN
                                    INSERT (Id, Name, Slug, Tier, EntraTenantId)
                                    VALUES (@Id, @Name, @Slug, @Tier, NULL);
                                """;

        object param = new
        {
            Id = tenantId,
            Name = "ArchLucid persistence contract governance",
            Slug = slug,
            Tier = TenantTier.Standard.ToString()
        };

        await connection.ExecuteAsync(
            new CommandDefinition(
                commandText: mergeSql,
                parameters: param,
                transaction: transaction,
                commandTimeout: null,
                commandType: null,
                flags: CommandFlags.Buffered,
                cancellationToken: cancellationToken));

        int present = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                commandText: """
                             SELECT COUNT(1)
                             FROM dbo.Tenants
                             WHERE Id = @Id;
                             """,
                parameters: new { Id = tenantId },
                transaction: transaction,
                commandTimeout: null,
                commandType: null,
                flags: CommandFlags.Buffered,
                cancellationToken: cancellationToken));


        if (present != 1)
            throw new InvalidOperationException(
                "Governance contract priming expected exactly one Tenants row for Id "
                + tenantId.ToString("N") + " but found " + present.ToString() + ".");
    }

    /// <summary>
    ///     Exclusive transaction-scoped app lock so parallel governance contract tests cannot interleave MERGE/INSERT vs
    ///     another session deleting or racing on <c>dbo.Tenants</c> (shared CI catalog).
    /// </summary>
    private static async Task AcquireGovernanceContractTenantMergeLockAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string lockSql = """
                               DECLARE @rc int;
                               EXEC @rc = sp_getapplock
                                   @Resource = @LockResource,
                                   @LockMode = N'Exclusive',
                                   @LockOwner = N'Transaction',
                                   @LockTimeout = 60000;
                               IF @rc NOT IN (0, 1)
                                   THROW 50002, N'sp_getapplock failed for governance contract tenant merge.', 1;
                               """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                commandText: lockSql,
                parameters: new { LockResource = GovernanceContractTenantMergeAppLockResource },
                transaction: transaction,
                commandTimeout: null,
                commandType: null,
                flags: CommandFlags.Buffered,
                cancellationToken: cancellationToken));
    }

    /// <summary>
    ///     Ensures <see cref="GovernanceRepositoryContractScope.TenantId" /> exists in <c>dbo.Tenants</c> (migration 118 FK).
    ///     Uses <c>MERGE</c> with <c>HOLDLOCK</c> so priming stays atomic vs parallel deletes under the default isolation level.
    ///     Governance SQL contract tests open connections with <see cref="RlsBypassTestDbConnectionFactory" /> so pooled
    ///     <c>SESSION_CONTEXT</c> does not block inserts or confuse FK checks.
    /// </summary>
    public static async Task PrimeGovernanceContractTenantAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        RlsBypassTestDbConnectionFactory factory = new(connectionString);
        await using SqlConnection connection =
            (SqlConnection)await factory.CreateOpenConnectionAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        Guid tenantId = GovernanceRepositoryContractScope.TenantId;

        try
        {
            await MergeGovernanceContractTenantAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // Second connection: ensures commit is visible outside the serializable transaction (shared CI catalogs).
        await using SqlConnection verify =
            (SqlConnection)await factory.CreateOpenConnectionAsync(cancellationToken);
        int visible = await verify.QuerySingleAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM dbo.Tenants
                WHERE Id = @Id;
                """,
                new { Id = tenantId },
                cancellationToken: cancellationToken));


        if (visible != 1)
            throw new InvalidOperationException(
                "Governance contract priming committed but tenant "
                + tenantId.ToString("N") + " was not visible on follow-up connect (count "
                + visible.ToString() + ").");
    }
}
