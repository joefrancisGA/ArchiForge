using System.Data;

using ArchiForge.Data.Infrastructure;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace ArchiForge.Data.Repositories;

/// <summary>
/// Dapper implementation for <see cref="IArchitectureRunIdempotencyRepository"/> (SQL Server and SQLite).
/// </summary>
public sealed class ArchitectureRunIdempotencyRepository(IDbConnectionFactory connectionFactory)
    : IArchitectureRunIdempotencyRepository
{
    /// <inheritdoc />
    public async Task<ArchitectureRunIdempotencyLookup?> TryGetAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKeyHash);

        const string sql = """
            SELECT RunId, RequestFingerprint
            FROM ArchitectureRunIdempotency
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND IdempotencyKeyHash = @IdempotencyKeyHash;
            """;

        using IDbConnection connection = await connectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        object parameters = CreateScopeParameters(connection, tenantId, workspaceId, projectId, idempotencyKeyHash);

        ArchitectureRunIdempotencyRow? row = await connection
            .QueryFirstOrDefaultAsync<ArchitectureRunIdempotencyRow>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (row is null)
            return null;

        return new ArchitectureRunIdempotencyLookup
        {
            RunId = row.RunId,
            RequestFingerprint = row.RequestFingerprint ?? []
        };
    }

    /// <inheritdoc />
    public async Task<bool> TryInsertAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        byte[] requestFingerprint,
        string runId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKeyHash);
        ArgumentNullException.ThrowIfNull(requestFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        const string sql = """
            INSERT INTO ArchitectureRunIdempotency
            (
                TenantId,
                WorkspaceId,
                ProjectId,
                IdempotencyKeyHash,
                RequestFingerprint,
                RunId,
                CreatedUtc
            )
            VALUES
            (
                @TenantId,
                @WorkspaceId,
                @ProjectId,
                @IdempotencyKeyHash,
                @RequestFingerprint,
                @RunId,
                @CreatedUtc
            );
            """;

        using IDbConnection connection = await connectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        object parameters = CreateInsertParameters(
            connection,
            tenantId,
            workspaceId,
            projectId,
            idempotencyKeyHash,
            requestFingerprint,
            runId);

        try
        {
            int affected = await connection
                .ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (affected > 0)
                return true;

            // Dapper + Microsoft.Data.Sqlite sometimes report 0 rows changed for INSERT even when the row exists.
            // Treating that as "lost race" makes CreateRunAsync call ResolveIdempotencyRaceAsync / Rehydrate and can surface DbException (503).
            if (connection is SqliteConnection)
            {
                return await RowExistsForScopeAsync(
                    connection,
                    tenantId,
                    workspaceId,
                    projectId,
                    idempotencyKeyHash,
                    cancellationToken).ConfigureAwait(false);
            }

            return false;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return false;
        }
        // Microsoft.Data.Sqlite exposes integer codes; avoid SqliteErrorCode enum (not available in all TFMs/package facades).
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19 || ex.SqliteExtendedErrorCode == 2067)
        {
            // 19 = SQLITE_CONSTRAINT; 2067 = SQLITE_CONSTRAINT_UNIQUE (duplicate key on INSERT).
            return false;
        }
    }

    private static async Task<bool> RowExistsForScopeAsync(
        IDbConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        CancellationToken cancellationToken)
    {
        const string existsSql = """
            SELECT COUNT(1)
            FROM ArchitectureRunIdempotency
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND IdempotencyKeyHash = @IdempotencyKeyHash;
            """;

        object scopeParameters = CreateScopeParameters(connection, tenantId, workspaceId, projectId, idempotencyKeyHash);

        object? scalar = await connection.ExecuteScalarAsync(new CommandDefinition(
            existsSql,
            scopeParameters,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return scalar is not null && Convert.ToInt64(scalar, System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static object CreateScopeParameters(
        IDbConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash)
    {
        if (connection is SqliteConnection)
        {
            return new
            {
                TenantId = tenantId.ToString("D"),
                WorkspaceId = workspaceId.ToString("D"),
                ProjectId = projectId.ToString("D"),
                IdempotencyKeyHash = idempotencyKeyHash
            };
        }

        return new
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            IdempotencyKeyHash = idempotencyKeyHash
        };
    }

    private static object CreateInsertParameters(
        IDbConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        byte[] requestFingerprint,
        string runId)
    {
        DateTime createdUtc = DateTime.UtcNow;

        if (connection is SqliteConnection)
        {
            return new
            {
                TenantId = tenantId.ToString("D"),
                WorkspaceId = workspaceId.ToString("D"),
                ProjectId = projectId.ToString("D"),
                IdempotencyKeyHash = idempotencyKeyHash,
                RequestFingerprint = requestFingerprint,
                RunId = runId,
                CreatedUtc = createdUtc.ToString("O")
            };
        }

        return new
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            IdempotencyKeyHash = idempotencyKeyHash,
            RequestFingerprint = requestFingerprint,
            RunId = runId,
            CreatedUtc = createdUtc
        };
    }

    private sealed class ArchitectureRunIdempotencyRow
    {
        public string RunId { get; init; } = string.Empty;

        public byte[]? RequestFingerprint { get; init; }
    }
}
