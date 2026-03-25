using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Governance;

/// <summary>
/// SQL Server implementation of <see cref="IPolicyPackAssignmentRepository"/> using Dapper against <c>dbo.PolicyPackAssignments</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>List semantics:</strong> Returns tenant-wide rows, workspace rows matching <paramref name="workspaceId"/>, and project rows matching
/// both workspace and <paramref name="projectId"/>. Aligns with <see cref="InMemoryPolicyPackAssignmentRepository"/> and
/// <see cref="EffectiveGovernanceResolver"/> filtering.
/// </para>
/// <para>
/// <strong>Callers:</strong> <see cref="PolicyPackResolver"/> and <see cref="EffectiveGovernanceResolver"/> via DI;
/// assignment writes from <c>PolicyPackManagementService</c>.
/// </para>
/// </remarks>
public sealed class DapperPolicyPackAssignmentRepository(ISqlConnectionFactory connectionFactory)
    : IPolicyPackAssignmentRepository
{
    /// <inheritdoc />
    /// <remarks>Inserts all columns including <c>ScopeLevel</c> and <c>IsPinned</c> (Change 46 schema).</remarks>
    public async Task CreateAsync(PolicyPackAssignment assignment, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.PolicyPackAssignments
            (
                AssignmentId, TenantId, WorkspaceId, ProjectId,
                PolicyPackId, PolicyPackVersion, IsEnabled, ScopeLevel, IsPinned, AssignedUtc
            )
            VALUES
            (
                @AssignmentId, @TenantId, @WorkspaceId, @ProjectId,
                @PolicyPackId, @PolicyPackVersion, @IsEnabled, @ScopeLevel, @IsPinned, @AssignedUtc
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, assignment, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>Currently only toggles <c>IsEnabled</c>; other columns require future migration if editable.</remarks>
    public async Task UpdateAsync(PolicyPackAssignment assignment, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.PolicyPackAssignments
            SET IsEnabled = @IsEnabled
            WHERE AssignmentId = @AssignmentId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, assignment, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Does not filter <c>IsEnabled</c> in SQL—callers filter so disabled rows can be listed by future admin APIs if needed.
    /// Ordered by <c>AssignedUtc DESC</c> for stable “newest first” UX.
    /// </remarks>
    public async Task<IReadOnlyList<PolicyPackAssignment>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200 *
            FROM dbo.PolicyPackAssignments
            WHERE TenantId = @TenantId
              AND (
                    (ScopeLevel = N'Tenant')
                 OR (ScopeLevel = N'Workspace' AND WorkspaceId = @WorkspaceId)
                 OR (ScopeLevel = N'Project' AND WorkspaceId = @WorkspaceId AND ProjectId = @ProjectId)
              )
            ORDER BY AssignedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<PolicyPackAssignment> rows = await connection.QueryAsync<PolicyPackAssignment>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                cancellationToken: ct));
        return rows.ToList();
    }
}
