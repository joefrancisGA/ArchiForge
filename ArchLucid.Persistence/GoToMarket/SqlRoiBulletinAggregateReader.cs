using ArchLucid.Core.GoToMarket;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.GoToMarket;

/// <summary>Dapper implementation: tenant-supplied baseline hours only (non-null <c>BaselineReviewCycleHours</c>).</summary>
public sealed class SqlRoiBulletinAggregateReader(ISqlConnectionFactory connectionFactory) : IRoiBulletinAggregateReader
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<RoiBulletinAggregateReadResult> ReadAsync(
        RoiBulletinQuarterWindow window,
        int minimumTenantsRequired,
        CancellationToken cancellationToken = default)
    {
        if (minimumTenantsRequired < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumTenantsRequired));

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string countSql = """
                                SELECT COUNT(1)
                                FROM dbo.Tenants
                                WHERE BaselineReviewCycleHours IS NOT NULL
                                  AND BaselineReviewCycleCapturedUtc >= @Start
                                  AND BaselineReviewCycleCapturedUtc < @End;
                                """;

        int count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                countSql,
                new
                {
                    Start = window.StartUtcInclusive,
                    End = window.EndUtcExclusive,
                },
                cancellationToken: cancellationToken));

        if (count < minimumTenantsRequired)
        {
            return new RoiBulletinAggregateReadResult(
                IsSufficientSample: false,
                TenantCount: count,
                MeanBaselineHours: null,
                MedianBaselineHours: null,
                P90BaselineHours: null,
                QuarterLabel: window.Label);
        }

        const string statsSql = """
                                SELECT
                                    AVG(CAST(BaselineReviewCycleHours AS FLOAT)) AS MeanH,
                                    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY BaselineReviewCycleHours) AS P50,
                                    PERCENTILE_CONT(0.9) WITHIN GROUP (ORDER BY BaselineReviewCycleHours) AS P90
                                FROM dbo.Tenants
                                WHERE BaselineReviewCycleHours IS NOT NULL
                                  AND BaselineReviewCycleCapturedUtc >= @Start
                                  AND BaselineReviewCycleCapturedUtc < @End;
                                """;

        StatsRow? row = await connection.QuerySingleOrDefaultAsync<StatsRow>(
            new CommandDefinition(
                statsSql,
                new
                {
                    Start = window.StartUtcInclusive,
                    End = window.EndUtcExclusive,
                },
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return new RoiBulletinAggregateReadResult(
                IsSufficientSample: false,
                TenantCount: count,
                MeanBaselineHours: null,
                MedianBaselineHours: null,
                P90BaselineHours: null,
                QuarterLabel: window.Label);
        }

        return new RoiBulletinAggregateReadResult(
            IsSufficientSample: true,
            TenantCount: count,
            MeanBaselineHours: ToDecimal(row.MeanH),
            MedianBaselineHours: ToDecimal(row.P50),
            P90BaselineHours: ToDecimal(row.P90),
            QuarterLabel: window.Label);
    }

    private static decimal? ToDecimal(double? value)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        return Convert.ToDecimal(value.Value);
    }

    private sealed class StatsRow
    {
        public double? MeanH
        {
            get; init;
        }

        public double? P50
        {
            get; init;
        }

        public double? P90
        {
            get; init;
        }
    }
}
