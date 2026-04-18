using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Governance;

/// <summary>
/// SQL Server persistence for <see cref="PolicyPackVersion"/> rows (<c>dbo.PolicyPackVersions</c>).
/// </summary>
/// <remarks>
/// Version column is quoted as <c>[Version]</c> in T-SQL. Used by publish, assign preflight (<c>PolicyPacksAppService.TryAssignAsync</c>),
/// and <c>PolicyPacksController.ListVersions</c>.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperPolicyPackVersionRepository(
    ISqlConnectionFactory connectionFactory,
    IGovernanceResolutionReadConnectionFactory governanceResolutionReadConnectionFactory)
    : IPolicyPackVersionRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(
        PolicyPackVersion version,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(version);

        const string sql = """
            INSERT INTO dbo.PolicyPackVersions
            (PolicyPackVersionId, PolicyPackId, [Version], ContentJson, CreatedUtc, IsPublished)
            VALUES
            (@PolicyPackVersionId, @PolicyPackId, @Version, @ContentJson, @CreatedUtc, @IsPublished);
            """;

        (SqlConnection conn, bool ownsConnection) =
            await SqlExternalConnection.ResolveAsync(connectionFactory, connection, ct);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, version, transaction: transaction, cancellationToken: ct));
        }
        finally
        {
            SqlExternalConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(version);

        const string sql = """
            UPDATE dbo.PolicyPackVersions
            SET ContentJson = @ContentJson, IsPublished = @IsPublished
            WHERE PolicyPackVersionId = @PolicyPackVersionId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, version, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>Case-sensitive match on stored version string; API normalizes input via validators.</remarks>
    public async Task<PolicyPackVersion?> GetByPackAndVersionAsync(
        Guid policyPackId,
        string version,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        const string sql = """
            SELECT PolicyPackVersionId, PolicyPackId, [Version] AS Version, ContentJson, CreatedUtc, IsPublished
            FROM dbo.PolicyPackVersions
            WHERE PolicyPackId = @PolicyPackId AND [Version] = @Ver;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<PolicyPackVersion>(
            new CommandDefinition(
                sql,
                new
                {
                    PolicyPackId = policyPackId,
                    Ver = version
                },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<(PolicyPackVersion Version, string? PreviousContentJson)> UpsertPublishedVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        const string selectSql = """
            SELECT PolicyPackVersionId, PolicyPackId, [Version] AS Version, ContentJson, CreatedUtc, IsPublished
            FROM dbo.PolicyPackVersions WITH (UPDLOCK, HOLDLOCK)
            WHERE PolicyPackId = @PolicyPackId AND [Version] = @Ver;
            """;

        const string updateSql = """
            UPDATE dbo.PolicyPackVersions
            SET ContentJson = @ContentJson, IsPublished = @IsPublished
            WHERE PolicyPackVersionId = @PolicyPackVersionId;
            """;

        const string insertSql = """
            INSERT INTO dbo.PolicyPackVersions
            (PolicyPackVersionId, PolicyPackId, [Version], ContentJson, CreatedUtc, IsPublished)
            VALUES
            (@PolicyPackVersionId, @PolicyPackId, @Version, @ContentJson, @CreatedUtc, @IsPublished);
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            PolicyPackVersion? existing = await connection.QueryFirstOrDefaultAsync<PolicyPackVersion>(
                new CommandDefinition(
                    selectSql,
                    new
                    {
                        PolicyPackId = policyPackId,
                        Ver = version
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            if (existing is not null)
            {
                string? previous = existing.ContentJson;
                existing.ContentJson = contentJson;
                existing.IsPublished = true;

                await connection.ExecuteAsync(
                    new CommandDefinition(updateSql, existing, transaction: transaction, cancellationToken: ct));

                await transaction.CommitAsync(ct);

                return (existing, previous);
            }

            PolicyPackVersion inserted = new()
            {
                PolicyPackVersionId = Guid.NewGuid(),
                PolicyPackId = policyPackId,
                Version = version,
                ContentJson = contentJson,
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            };

            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, inserted, transaction: transaction, cancellationToken: ct));

            await transaction.CommitAsync(ct);

            return (inserted, null);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(Guid policyPackId, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200 PolicyPackVersionId, PolicyPackId, [Version] AS Version, ContentJson, CreatedUtc, IsPublished
            FROM dbo.PolicyPackVersions
            WHERE PolicyPackId = @PolicyPackId
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await governanceResolutionReadConnectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<PolicyPackVersion> rows = await connection.QueryAsync<PolicyPackVersion>(
            new CommandDefinition(sql, new
            {
                PolicyPackId = policyPackId
            }, cancellationToken: ct));
        return rows.ToList();
    }
}
