using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// SQL Server implementation of <see cref="IAdvisoryScanExecutionRepository"/> against <c>dbo.AdvisoryScanExecutions</c>.
/// </summary>
/// <remarks>Registered scoped in DI when SQL storage is enabled.</remarks>
public sealed class DapperAdvisoryScanExecutionRepository(ISqlConnectionFactory connectionFactory)
    : IAdvisoryScanExecutionRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AdvisoryScanExecution execution, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(execution);
        const string sql = """
            INSERT INTO dbo.AdvisoryScanExecutions
            (
                ExecutionId, ScheduleId, TenantId, WorkspaceId, ProjectId,
                StartedUtc, CompletedUtc, Status, ResultJson, ErrorMessage
            )
            VALUES
            (
                @ExecutionId, @ScheduleId, @TenantId, @WorkspaceId, @ProjectId,
                @StartedUtc, @CompletedUtc, @Status, @ResultJson, @ErrorMessage
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, execution, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AdvisoryScanExecution execution, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(execution);
        const string sql = """
            UPDATE dbo.AdvisoryScanExecutions
            SET
                CompletedUtc = @CompletedUtc,
                Status = @Status,
                ResultJson = @ResultJson,
                ErrorMessage = @ErrorMessage
            WHERE ExecutionId = @ExecutionId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, execution, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdvisoryScanExecution>> ListByScheduleAsync(
        Guid scheduleId,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                ExecutionId, ScheduleId, TenantId, WorkspaceId, ProjectId,
                StartedUtc, CompletedUtc, Status, ResultJson, ErrorMessage
            FROM dbo.AdvisoryScanExecutions
            WHERE ScheduleId = @ScheduleId
            ORDER BY StartedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AdvisoryScanExecution> result = await connection.QueryAsync<AdvisoryScanExecution>(
            new CommandDefinition(sql, new
            {
                ScheduleId = scheduleId,
                Take = Math.Clamp(take, 1, 200)
            }, cancellationToken: ct));

        return result.ToList();
    }
}
