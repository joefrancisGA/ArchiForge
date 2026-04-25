using ArchLucid.Core.Metering;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Interfaces;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy;

public sealed class DapperUsageEventRepository(ISqlConnectionFactory connectionFactory) : IUsageEventRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task InsertAsync(UsageEvent usageEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(usageEvent);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           INSERT INTO dbo.UsageEvents (Id, TenantId, WorkspaceId, ProjectId, Kind, Quantity, RecordedUtc, CorrelationId)
                           VALUES (@Id, @TenantId, @WorkspaceId, @ProjectId, @Kind, @Quantity, @RecordedUtc, @CorrelationId);
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    usageEvent.Id,
                    usageEvent.TenantId,
                    usageEvent.WorkspaceId,
                    usageEvent.ProjectId,
                    Kind = UsageMeterKindSql.ToKindString(usageEvent.Kind),
                    usageEvent.Quantity,
                    usageEvent.RecordedUtc,
                    usageEvent.CorrelationId
                },
                cancellationToken: ct));
    }

    public async Task InsertBatchAsync(IReadOnlyList<UsageEvent> events, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
            return;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           INSERT INTO dbo.UsageEvents (Id, TenantId, WorkspaceId, ProjectId, Kind, Quantity, RecordedUtc, CorrelationId)
                           VALUES (@Id, @TenantId, @WorkspaceId, @ProjectId, @Kind, @Quantity, @RecordedUtc, @CorrelationId);
                           """;

        foreach (UsageEvent e in events)

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        e.Id,
                        e.TenantId,
                        e.WorkspaceId,
                        e.ProjectId,
                        Kind = UsageMeterKindSql.ToKindString(e.Kind),
                        e.Quantity,
                        e.RecordedUtc,
                        e.CorrelationId
                    },
                    cancellationToken: ct));
    }

    public async Task<IReadOnlyList<TenantUsageSummary>> AggregateByKindAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT TenantId, Kind, SUM(Quantity) AS TotalQuantity, @PeriodStart AS PeriodStartUtc, @PeriodEnd AS PeriodEndUtc
                           FROM dbo.UsageEvents
                           WHERE TenantId = @TenantId
                             AND RecordedUtc >= @PeriodStart
                             AND RecordedUtc < @PeriodEnd
                           GROUP BY TenantId, Kind;
                           """;

        IEnumerable<SummaryRow> rows = await connection.QueryAsync<SummaryRow>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, PeriodStart = periodStart, PeriodEnd = periodEnd },
                cancellationToken: ct));

        return rows
            .Select(static r => new TenantUsageSummary
            {
                TenantId = r.TenantId,
                Kind = UsageMeterKindSql.ParseKind(r.Kind),
                TotalQuantity = r.TotalQuantity,
                PeriodStartUtc = r.PeriodStartUtc,
                PeriodEndUtc = r.PeriodEndUtc
            })
            .ToList();
    }

    public async Task<IReadOnlyList<UsageEvent>> ListAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        UsageMeterKind? kindFilter,
        int take,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        string sql = """
                     SELECT TOP (@Take) Id, TenantId, WorkspaceId, ProjectId, Kind, Quantity, RecordedUtc, CorrelationId
                     FROM dbo.UsageEvents
                     WHERE TenantId = @TenantId
                       AND RecordedUtc >= @PeriodStart
                       AND RecordedUtc < @PeriodEnd
                     """;

        object parameters;

        if (kindFilter.HasValue)
        {
            sql += " AND Kind = @Kind ";
            parameters = new
            {
                Take = take,
                TenantId = tenantId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Kind = UsageMeterKindSql.ToKindString(kindFilter.Value)
            };
        }
        else

            parameters = new { Take = take, TenantId = tenantId, PeriodStart = periodStart, PeriodEnd = periodEnd };


        sql += " ORDER BY RecordedUtc DESC;";

        IEnumerable<EventRow> rows = await connection.QueryAsync<EventRow>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));

        return rows.Select(static r => r.ToUsageEvent()).ToList();
    }

    private sealed class SummaryRow
    {
        public Guid TenantId
        {
            get;
            init;
        }

        public string Kind
        {
            get;
            init;
        } = string.Empty;

        public long TotalQuantity
        {
            get;
            init;
        }

        public DateTimeOffset PeriodStartUtc
        {
            get;
            init;
        }

        public DateTimeOffset PeriodEndUtc
        {
            get;
            init;
        }
    }

    private sealed class EventRow
    {
        public Guid Id
        {
            get;
            init;
        }

        public Guid TenantId
        {
            get;
            init;
        }

        public Guid WorkspaceId
        {
            get;
            init;
        }

        public Guid ProjectId
        {
            get;
            init;
        }

        public string Kind
        {
            get;
            init;
        } = string.Empty;

        public long Quantity
        {
            get;
            init;
        }

        public DateTimeOffset RecordedUtc
        {
            get;
            init;
        }

        public string? CorrelationId
        {
            get;
            init;
        }

        internal UsageEvent ToUsageEvent()
        {
            return new UsageEvent
            {
                Id = Id,
                TenantId = TenantId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                Kind = UsageMeterKindSql.ParseKind(Kind),
                Quantity = Quantity,
                RecordedUtc = RecordedUtc,
                CorrelationId = CorrelationId
            };
        }
    }
}
