using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Connections;

using Dapper;

namespace ArchiForge.Persistence.Advisory;

/// <inheritdoc cref="IArchitectureDigestRepository" />
public sealed class DapperArchitectureDigestRepository(ISqlConnectionFactory connectionFactory)
    : IArchitectureDigestRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(ArchitectureDigest digest, CancellationToken ct)
    {
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

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, digest, cancellationToken: ct));
    }

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
                Title, Summary, ContentMarkdown, MetadataJson
            FROM dbo.ArchitectureDigests
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY GeneratedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<ArchitectureDigest>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Take = take
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
                Title, Summary, ContentMarkdown, MetadataJson
            FROM dbo.ArchitectureDigests
            WHERE DigestId = @DigestId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<ArchitectureDigest>(
            new CommandDefinition(sql, new
            {
                DigestId = digestId
            }, cancellationToken: ct));
    }
}
