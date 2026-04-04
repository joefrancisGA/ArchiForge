using System.Diagnostics.CodeAnalysis;

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
/// <strong>List semantics:</strong> Returns tenant-wide rows, workspace rows matching the caller's workspace id, and project rows
/// matching both workspace and project id. Aligns with <see cref="InMemoryPolicyPackAssignmentRepository"/> and
/// <see cref="EffectiveGovernanceResolver"/> filtering.
/// </para>
/// <para>
/// <strong>Callers:</strong> <see cref="PolicyPackResolver"/> and <see cref="EffectiveGovernanceResolver"/> via DI;
/// assignment writes from <c>PolicyPackManagementService</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperPolicyPackAssignmentRepository(
    ISqlConnectionFactory connectionFactory,
    IGovernanceResolutionReadConnectionFactory governanceResolutionReadConnectionFactory)
    : IPolicyPackAssignmentRepository
{
    /// <inheritdoc />
    /// <remarks>Inserts all columns including <c>ScopeLevel</c> and <c>IsPinned</c> (Change 46 schema).</remarks>
    public async Task CreateAsync(PolicyPackAssignment assignment, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        const string sql = """
            INSERT INTO dbo.PolicyPackAssignments
            (
                AssignmentId, TenantId, WorkspaceId, ProjectId,
                PolicyPackId, PolicyPackVersion, IsEnabled, ScopeLevel, IsPinned, AssignedUtc, ArchivedUtc
            )
            VALUES
            (
                @AssignmentId, @TenantId, @WorkspaceId, @ProjectId,
                @PolicyPackId, @PolicyPackVersion, @IsEnabled, @ScopeLevel, @IsPinned, @AssignedUtc, @ArchivedUtc
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, assignment, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>Currently only toggles <c>IsEnabled</c>; other columns require future migration if editable.</remarks>
    public async Task UpdateAsync(PolicyPackAssignment assignment, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assignment);

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
            SELECT TOP 200
                AssignmentId, TenantId, WorkspaceId, ProjectId,
                PolicyPackId, PolicyPackVersion, IsEnabled, ScopeLevel, IsPinned, AssignedUtc, ArchivedUtc
            FROM dbo.PolicyPackAssignments
            WHERE TenantId = @TenantId
              AND ArchivedUtc IS NULL
              AND (
                    (ScopeLevel = N'Tenant')
                 OR (ScopeLevel = N'Workspace' AND WorkspaceId = @WorkspaceId)
                 OR (ScopeLevel = N'Project' AND WorkspaceId = @WorkspaceId AND ProjectId = @ProjectId)
              )
            ORDER BY AssignedUtc DESC;
            """;

        await using SqlConnection connection = await governanceResolutionReadConnectionFactory.CreateOpenConnectionAsync(ct);
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

    /// <inheritdoc />
    public async Task<bool> ArchiveAsync(Guid tenantId, Guid assignmentId, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.PolicyPackAssignments
            SET ArchivedUtc = SYSUTCDATETIME()
            WHERE AssignmentId = @AssignmentId
              AND TenantId = @TenantId
              AND ArchivedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        int affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { AssignmentId = assignmentId, TenantId = tenantId }, cancellationToken: ct));
        return affected > 0;
    }
}
