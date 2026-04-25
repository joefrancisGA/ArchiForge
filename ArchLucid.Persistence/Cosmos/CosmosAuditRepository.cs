using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;

using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Audit;

using Microsoft.Azure.Cosmos;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Cosmos-backed <see cref="IAuditRepository" />.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires Cosmos account or emulator.")]
public sealed class CosmosAuditRepository(CosmosClientFactory clientFactory) : IAuditRepository
{
    private const string ContainerId = "audit-events";

    private readonly CosmosClientFactory _clientFactory =
        clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

    /// <inheritdoc />
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        AuditEventDocument doc = ToDocument(auditEvent);

        try
        {
            await container.CreateItemAsync(doc, new PartitionKey(doc.TenantId), cancellationToken: ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            // Idempotent append when EventId is replayed.
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEvent>> GetByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        int clamped = Math.Clamp(take <= 0 ? 100 : take, 1, 500);
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        string tid = tenantId.ToString("D");
        string wid = workspaceId.ToString("D");
        string pid = projectId.ToString("D");

        QueryDefinition query = new QueryDefinition(
                """
                SELECT * FROM c
                WHERE c.workspaceId = @wid AND c.projectId = @pid
                ORDER BY c.occurredUtc DESC, c.id DESC
                """)
            .WithParameter("@wid", wid)
            .WithParameter("@pid", pid);

        using FeedIterator<AuditEventDocument> iterator = container.GetItemQueryIterator<AuditEventDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tid), MaxItemCount = clamped });

        List<AuditEvent> list = [];

        while (iterator.HasMoreResults && list.Count < clamped)
        {
            FeedResponse<AuditEventDocument> page = await iterator.ReadNextAsync(ct);

            foreach (AuditEventDocument doc in page)
            {
                list.Add(ToEvent(doc));

                if (list.Count >= clamped)
                    break;
            }
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEvent>> GetFilteredAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        AuditEventFilter filter,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);

        int take = Math.Clamp(filter.Take <= 0 ? 100 : filter.Take, 1, 500);
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        string tid = tenantId.ToString("D");
        string wid = workspaceId.ToString("D");
        string pid = projectId.ToString("D");

        QueryDefinition query = BuildFilteredQuery(wid, pid, filter);

        using FeedIterator<AuditEventDocument> iterator = container.GetItemQueryIterator<AuditEventDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tid), MaxItemCount = take });

        List<AuditEvent> list = [];

        while (iterator.HasMoreResults && list.Count < take)
        {
            FeedResponse<AuditEventDocument> page = await iterator.ReadNextAsync(ct);

            foreach (AuditEventDocument doc in page)
            {
                list.Add(ToEvent(doc));

                if (list.Count >= take)
                    break;
            }
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEvent>> GetExportAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxRows,
        CancellationToken ct)
    {
        int take = Math.Clamp(maxRows <= 0 ? 10_000 : maxRows, 1, 10_000);
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        string tid = tenantId.ToString("D");
        string wid = workspaceId.ToString("D");
        string pid = projectId.ToString("D");
        string fromIso = FormatUtcIso(fromUtc);
        string toIso = FormatUtcIso(toUtc);

        QueryDefinition query = new QueryDefinition(
                """
                SELECT * FROM c
                WHERE c.workspaceId = @wid AND c.projectId = @pid
                  AND c.occurredUtc >= @fromUtc AND c.occurredUtc < @toUtc
                ORDER BY c.occurredUtc ASC, c.id ASC
                """)
            .WithParameter("@wid", wid)
            .WithParameter("@pid", pid)
            .WithParameter("@fromUtc", fromIso)
            .WithParameter("@toUtc", toIso);

        using FeedIterator<AuditEventDocument> iterator = container.GetItemQueryIterator<AuditEventDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tid), MaxItemCount = take });

        List<AuditEvent> list = [];

        while (iterator.HasMoreResults && list.Count < take)
        {
            FeedResponse<AuditEventDocument> page = await iterator.ReadNextAsync(ct);

            foreach (AuditEventDocument doc in page)
            {
                list.Add(ToEvent(doc));

                if (list.Count >= take)
                    break;
            }
        }

        return list;
    }

    private static QueryDefinition BuildFilteredQuery(string wid, string pid, AuditEventFilter filter)
    {
        StringBuilder sql = new(
            """
            SELECT * FROM c
            WHERE c.workspaceId = @wid AND c.projectId = @pid
            """);

        List<KeyValuePair<string, object?>> parameters =
        [
            new("@wid", wid),
            new("@pid", pid)
        ];

        if (!string.IsNullOrWhiteSpace(filter.EventType))
        {
            sql.Append(" AND c.eventType = @eventType");
            parameters.Add(new KeyValuePair<string, object?>("@eventType", filter.EventType.Trim()));
        }

        if (filter.FromUtc.HasValue)
        {
            sql.Append(" AND c.occurredUtc >= @fromUtc");
            parameters.Add(new KeyValuePair<string, object?>("@fromUtc", FormatUtcIso(filter.FromUtc.Value)));
        }

        if (filter.ToUtc.HasValue)
        {
            sql.Append(" AND c.occurredUtc <= @toUtc");
            parameters.Add(new KeyValuePair<string, object?>("@toUtc", FormatUtcIso(filter.ToUtc.Value)));
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            sql.Append(" AND c.correlationId = @correlationId");
            parameters.Add(new KeyValuePair<string, object?>("@correlationId", filter.CorrelationId.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorUserId))
        {
            sql.Append(" AND c.actorUserId = @actorUserId");
            parameters.Add(new KeyValuePair<string, object?>("@actorUserId", filter.ActorUserId.Trim()));
        }

        if (filter.RunId.HasValue)
        {
            sql.Append(" AND c.runId = @runId");
            parameters.Add(new KeyValuePair<string, object?>("@runId", filter.RunId.Value.ToString("D")));
        }

        if (filter.BeforeUtc.HasValue)
        {
            if (filter.BeforeEventId.HasValue)
            {
                sql.Append(
                    """
                     AND (
                        c.occurredUtc < @beforeUtc
                        OR (c.occurredUtc = @beforeUtc AND c.id < @beforeEventId)
                    )
                    """);
                parameters.Add(new KeyValuePair<string, object?>("@beforeUtc", FormatUtcIso(filter.BeforeUtc.Value)));
                parameters.Add(
                    new KeyValuePair<string, object?>("@beforeEventId", filter.BeforeEventId.Value.ToString("D")));
            }
            else
            {
                sql.Append(" AND c.occurredUtc < @beforeUtc");
                parameters.Add(new KeyValuePair<string, object?>("@beforeUtc", FormatUtcIso(filter.BeforeUtc.Value)));
            }
        }

        sql.Append(" ORDER BY c.occurredUtc DESC, c.id DESC");

        QueryDefinition query = new(sql.ToString());

        foreach (KeyValuePair<string, object?> pair in parameters)
            query = query.WithParameter(pair.Key, pair.Value!);

        return query;
    }

    private static string FormatUtcIso(DateTime value)
    {
        return value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
    }

    private static AuditEventDocument ToDocument(AuditEvent auditEvent)
    {
        return new AuditEventDocument
        {
            Id = auditEvent.EventId.ToString("D"),
            TenantId = auditEvent.TenantId.ToString("D"),
            WorkspaceId = auditEvent.WorkspaceId.ToString("D"),
            ProjectId = auditEvent.ProjectId.ToString("D"),
            OccurredUtc = FormatUtcIso(auditEvent.OccurredUtc),
            EventType = auditEvent.EventType,
            ActorUserId = auditEvent.ActorUserId,
            ActorUserName = auditEvent.ActorUserName,
            RunId = auditEvent.RunId?.ToString("D"),
            ManifestId = auditEvent.ManifestId?.ToString("D"),
            ArtifactId = auditEvent.ArtifactId?.ToString("D"),
            DataJson = string.IsNullOrEmpty(auditEvent.DataJson) ? "{}" : auditEvent.DataJson,
            CorrelationId = auditEvent.CorrelationId
        };
    }

    private static AuditEvent ToEvent(AuditEventDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        return new AuditEvent
        {
            EventId = Guid.Parse(doc.Id, CultureInfo.InvariantCulture),
            OccurredUtc = DateTime.Parse(doc.OccurredUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                .ToUniversalTime(),
            EventType = doc.EventType,
            ActorUserId = doc.ActorUserId,
            ActorUserName = doc.ActorUserName,
            TenantId = Guid.Parse(doc.TenantId, CultureInfo.InvariantCulture),
            WorkspaceId = Guid.Parse(doc.WorkspaceId, CultureInfo.InvariantCulture),
            ProjectId = Guid.Parse(doc.ProjectId, CultureInfo.InvariantCulture),
            RunId = string.IsNullOrEmpty(doc.RunId) ? null : Guid.Parse(doc.RunId, CultureInfo.InvariantCulture),
            ManifestId =
                string.IsNullOrEmpty(doc.ManifestId) ? null : Guid.Parse(doc.ManifestId, CultureInfo.InvariantCulture),
            ArtifactId =
                string.IsNullOrEmpty(doc.ArtifactId) ? null : Guid.Parse(doc.ArtifactId, CultureInfo.InvariantCulture),
            DataJson = string.IsNullOrEmpty(doc.DataJson) ? "{}" : doc.DataJson,
            CorrelationId = doc.CorrelationId
        };
    }
}
