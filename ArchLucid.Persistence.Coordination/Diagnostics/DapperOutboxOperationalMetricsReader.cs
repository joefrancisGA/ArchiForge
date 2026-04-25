using System.Diagnostics.CodeAnalysis;

using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Coordination.Diagnostics;

/// <summary>Single round-trip depth/age read for authority, retrieval, and integration outboxes.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent; integration environments exercise via host.")]
public sealed class DapperOutboxOperationalMetricsReader(ISqlConnectionFactory connectionFactory)
    : IOutboxOperationalMetricsReader
{
    /// <inheritdoc />
    public async Task<OutboxOperationalMetricsSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT COUNT_BIG(1) AS Cnt, MIN(CreatedUtc) AS OldestUtc
                           FROM dbo.AuthorityPipelineWorkOutbox
                           WHERE ProcessedUtc IS NULL;

                           SELECT COUNT_BIG(1) AS Cnt, MIN(CreatedUtc) AS OldestUtc
                           FROM dbo.RetrievalIndexingOutbox
                           WHERE ProcessedUtc IS NULL;

                           SELECT COUNT_BIG(1) AS Cnt, MIN(CreatedUtc) AS OldestUtc
                           FROM dbo.IntegrationEventOutbox
                           WHERE ProcessedUtc IS NULL
                             AND DeadLetteredUtc IS NULL
                             AND (NextRetryUtc IS NULL OR NextRetryUtc <= SYSUTCDATETIME());

                           SELECT COUNT_BIG(1) AS Cnt
                           FROM dbo.IntegrationEventOutbox
                           WHERE DeadLetteredUtc IS NOT NULL
                             AND ProcessedUtc IS NULL;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        SqlMapper.GridReader multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        Row auth = (await multi.ReadAsync<Row>()).FirstOrDefault() ?? new Row();
        Row retrieval = (await multi.ReadAsync<Row>()).FirstOrDefault() ?? new Row();
        Row integration = (await multi.ReadAsync<Row>()).FirstOrDefault() ?? new Row();
        DeadRow deadRow = (await multi.ReadAsync<DeadRow>()).FirstOrDefault() ?? new DeadRow();

        DateTime utcNow = DateTime.UtcNow;

        return new OutboxOperationalMetricsSnapshot
        {
            AuthorityPipelineWorkPending = auth.Cnt,
            AuthorityPipelineWorkOldestPendingAgeSeconds = AgeSeconds(auth.OldestUtc, utcNow),
            RetrievalIndexingOutboxPending = retrieval.Cnt,
            RetrievalIndexingOutboxOldestPendingAgeSeconds = AgeSeconds(retrieval.OldestUtc, utcNow),
            IntegrationEventOutboxPublishPending = integration.Cnt,
            IntegrationEventOutboxDeadLetter = deadRow.Cnt,
            IntegrationEventOutboxOldestActionablePendingAgeSeconds = AgeSeconds(integration.OldestUtc, utcNow)
        };
    }

    private static double AgeSeconds(DateTime? oldestUtc, DateTime utcNow)
    {
        if (!oldestUtc.HasValue)
            return 0;


        double seconds = (utcNow - oldestUtc.Value.ToUniversalTime()).TotalSeconds;

        return seconds < 0 ? 0 : seconds;
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Dapper.")]
    private sealed class Row
    {
        public long Cnt
        {
            get;
            init;
        }

        public DateTime? OldestUtc
        {
            get;
            init;
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Dapper.")]
    private sealed class DeadRow
    {
        public long Cnt
        {
            get;
            init;
        }
    }
}
