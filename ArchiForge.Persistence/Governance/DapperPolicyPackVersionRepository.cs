using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Persistence.Connections;

using Dapper;

namespace ArchiForge.Persistence.Governance;

/// <summary>
/// SQL Server persistence for <see cref="PolicyPackVersion"/> rows (<c>dbo.PolicyPackVersions</c>).
/// </summary>
/// <remarks>
/// Version column is quoted as <c>[Version]</c> in T-SQL. Used by publish, assign preflight (<c>PolicyPacksAppService.TryAssignAsync</c>),
/// and <c>PolicyPacksController.ListVersions</c>.
/// </remarks>
public sealed class DapperPolicyPackVersionRepository(ISqlConnectionFactory connectionFactory)
    : IPolicyPackVersionRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.PolicyPackVersions
            (PolicyPackVersionId, PolicyPackId, [Version], ContentJson, CreatedUtc, IsPublished)
            VALUES
            (@PolicyPackVersionId, @PolicyPackId, @Version, @ContentJson, @CreatedUtc, @IsPublished);
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, version, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PolicyPackVersion version, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.PolicyPackVersions
            SET ContentJson = @ContentJson, IsPublished = @IsPublished
            WHERE PolicyPackVersionId = @PolicyPackVersionId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, version, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>Case-sensitive match on stored version string; API normalizes input via validators.</remarks>
    public async Task<PolicyPackVersion?> GetByPackAndVersionAsync(
        Guid policyPackId,
        string version,
        CancellationToken ct)
    {
        const string sql = """
            SELECT PolicyPackVersionId, PolicyPackId, [Version] AS Version, ContentJson, CreatedUtc, IsPublished
            FROM dbo.PolicyPackVersions
            WHERE PolicyPackId = @PolicyPackId AND [Version] = @Ver;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
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
    public async Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(Guid policyPackId, CancellationToken ct)
    {
        const string sql = """
            SELECT PolicyPackVersionId, PolicyPackId, [Version] AS Version, ContentJson, CreatedUtc, IsPublished
            FROM dbo.PolicyPackVersions
            WHERE PolicyPackId = @PolicyPackId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<PolicyPackVersion>(
            new CommandDefinition(sql, new
            {
                PolicyPackId = policyPackId
            }, cancellationToken: ct));
        return rows.ToList();
    }
}
