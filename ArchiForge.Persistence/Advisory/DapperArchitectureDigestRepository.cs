using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IArchitectureDigestRepository" />
public sealed class DapperArchitectureDigestRepository(ISqlConnectionFactory connectionFactory)
    : IArchitectureDigestRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(ArchitectureDigest digest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(digest);

        const string sql = """
            INSERT INTO dbo.ArchitectureDigests
            (
                DigestId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, GeneratedUtc,
                Title, Summary, ContentMarkdown, MetadataJson
            )
            VALUES
            (
                @DigestId, @TenantId, @WorkspaceId, @ProjectId,
                @RunId, @ComparedToRunId, @GeneratedUtc,
                @Title, @Summary, @ContentMarkdown, @MetadataJson
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, digest, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArchitectureDigest>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                DigestId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, GeneratedUtc,
                Title, Summary, ContentMarkdown, MetadataJson, ArchivedUtc
            FROM dbo.ArchitectureDigests
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND ArchivedUtc IS NULL
            ORDER BY GeneratedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<ArchitectureDigest> result = await connection.QueryAsync<ArchitectureDigest>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Take = Math.Clamp(take, 1, 200)
                },
                cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<ArchitectureDigest?> GetByIdAsync(Guid digestId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                DigestId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, GeneratedUtc,
                Title, Summary, ContentMarkdown, MetadataJson, ArchivedUtc
            FROM dbo.ArchitectureDigests
            WHERE DigestId = @DigestId
              AND ArchivedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<ArchitectureDigest>(
            new CommandDefinition(sql, new
            {
                DigestId = digestId
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> ArchiveDigestsGeneratedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.ArchitectureDigests
            SET ArchivedUtc = SYSUTCDATETIME()
            WHERE ArchivedUtc IS NULL AND GeneratedUtc < @Cutoff;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Cutoff = cutoffUtc.UtcDateTime }, cancellationToken: ct));
    }
}
