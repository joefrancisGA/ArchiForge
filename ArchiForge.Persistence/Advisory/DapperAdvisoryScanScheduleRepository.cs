using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Connections;

using Dapper;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// SQL Server implementation of <see cref="IAdvisoryScanScheduleRepository"/> against <c>dbo.AdvisoryScanSchedules</c>.
/// </summary>
/// <remarks>Registered scoped in DI when SQL storage is enabled.</remarks>
public sealed class DapperAdvisoryScanScheduleRepository(ISqlConnectionFactory connectionFactory)
    : IAdvisoryScanScheduleRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AdvisoryScanSchedules
            (
                ScheduleId, TenantId, WorkspaceId, ProjectId, RunProjectSlug,
                Name, CronExpression, IsEnabled,
                CreatedUtc, LastRunUtc, NextRunUtc
            )
            VALUES
            (
                @ScheduleId, @TenantId, @WorkspaceId, @ProjectId, @RunProjectSlug,
                @Name, @CronExpression, @IsEnabled,
                @CreatedUtc, @LastRunUtc, @NextRunUtc
            );
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, schedule, cancellationToken: ct));
    }

    public async Task UpdateAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AdvisoryScanSchedules
            SET
                Name = @Name,
                CronExpression = @CronExpression,
                IsEnabled = @IsEnabled,
                RunProjectSlug = @RunProjectSlug,
                LastRunUtc = @LastRunUtc,
                NextRunUtc = @NextRunUtc
            WHERE ScheduleId = @ScheduleId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, schedule, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdvisoryScanSchedule>> ListDueAsync(
        DateTime utcNow,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                ScheduleId, TenantId, WorkspaceId, ProjectId, RunProjectSlug,
                Name, CronExpression, IsEnabled,
                CreatedUtc, LastRunUtc, NextRunUtc
            FROM dbo.AdvisoryScanSchedules
            WHERE IsEnabled = 1
              AND NextRunUtc IS NOT NULL
              AND NextRunUtc <= @UtcNow
            ORDER BY NextRunUtc ASC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<AdvisoryScanSchedule>(
            new CommandDefinition(sql, new
            {
                UtcNow = utcNow,
                Take = take
            }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdvisoryScanSchedule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                ScheduleId, TenantId, WorkspaceId, ProjectId, RunProjectSlug,
                Name, CronExpression, IsEnabled,
                CreatedUtc, LastRunUtc, NextRunUtc
            FROM dbo.AdvisoryScanSchedules
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var result = await connection.QueryAsync<AdvisoryScanSchedule>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<AdvisoryScanSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                ScheduleId, TenantId, WorkspaceId, ProjectId, RunProjectSlug,
                Name, CronExpression, IsEnabled,
                CreatedUtc, LastRunUtc, NextRunUtc
            FROM dbo.AdvisoryScanSchedules
            WHERE ScheduleId = @ScheduleId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<AdvisoryScanSchedule>(
            new CommandDefinition(sql, new
            {
                ScheduleId = scheduleId
            }, cancellationToken: ct));
    }
}
