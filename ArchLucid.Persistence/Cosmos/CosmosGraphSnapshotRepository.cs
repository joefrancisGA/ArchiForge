using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;

using Microsoft.Azure.Cosmos;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Cosmos-backed <see cref="IGraphSnapshotRepository" /> (single document per snapshot).</summary>
[ExcludeFromCodeCoverage(Justification = "Requires Cosmos account or emulator.")]
public sealed class CosmosGraphSnapshotRepository(CosmosClientFactory clientFactory) : IGraphSnapshotRepository
{
    private const string ContainerId = "graph-snapshots";

    private readonly CosmosClientFactory _clientFactory =
        clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

    /// <inheritdoc />
    public async Task SaveAsync(
        GraphSnapshot snapshot,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (connection is not null || transaction is not null)
            throw new InvalidOperationException(
                "Cosmos graph snapshots cannot participate in SQL transactions. "
                + "Ensure CosmosDb:GraphSnapshotsEnabled is coordinated with AuthorityPipelineStagesExecutor (non-transactional save), "
                + "or disable Cosmos graph snapshots.");

        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        GraphSnapshotDocument doc = ToDocument(snapshot);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.GraphSnapshotId), cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<GraphSnapshot?> GetByIdAsync(Guid graphSnapshotId, CancellationToken ct)
    {
        string pk = graphSnapshotId.ToString("D");
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);

        try
        {
            ItemResponse<GraphSnapshotDocument> response = await container.ReadItemAsync<GraphSnapshotDocument>(
                graphSnapshotId.ToString("D"),
                new PartitionKey(pk),
                cancellationToken: ct);

            return FromDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<GraphSnapshot?> GetLatestByContextSnapshotIdAsync(Guid contextSnapshotId, CancellationToken ct)
    {
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        string ctx = contextSnapshotId.ToString("D");
        QueryDefinition query = new QueryDefinition(
                """
                SELECT * FROM c
                WHERE c.contextSnapshotId = @ctx
                ORDER BY c.createdUtc DESC
                """)
            .WithParameter("@ctx", ctx);

        using FeedIterator<GraphSnapshotDocument> iterator = container.GetItemQueryIterator<GraphSnapshotDocument>(
            query,
            requestOptions: new QueryRequestOptions { MaxItemCount = 1 });

        if (!iterator.HasMoreResults)
            return null;

        FeedResponse<GraphSnapshotDocument> page = await iterator.ReadNextAsync(ct);
        GraphSnapshotDocument? doc = page.Resource.FirstOrDefault();

        return doc is null ? null : FromDocument(doc);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GraphSnapshotIndexedEdge>> ListIndexedEdgesAsync(Guid graphSnapshotId,
        CancellationToken ct)
    {
        GraphSnapshot? snapshot = await GetByIdAsync(graphSnapshotId, ct);

        if (snapshot is null)
            return [];

        return GraphSnapshotEdgeIndexer
            .BuildRows(snapshot)
            .Select(r => new GraphSnapshotIndexedEdge(r.EdgeId, r.FromNodeId, r.ToNodeId, r.EdgeType, r.Weight))
            .ToList();
    }

    private static GraphSnapshotDocument ToDocument(GraphSnapshot snapshot)
    {
        string gid = snapshot.GraphSnapshotId.ToString("D");

        return new GraphSnapshotDocument
        {
            Id = gid,
            GraphSnapshotId = gid,
            ContextSnapshotId = snapshot.ContextSnapshotId.ToString("D"),
            RunId = snapshot.RunId.ToString("D"),
            SchemaVersion = snapshot.SchemaVersion,
            CreatedUtc = snapshot.CreatedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            NodesJson = JsonEntitySerializer.Serialize(snapshot.Nodes),
            EdgesJson = JsonEntitySerializer.Serialize(snapshot.Edges),
            WarningsJson = JsonEntitySerializer.Serialize(snapshot.Warnings)
        };
    }

    private static GraphSnapshot FromDocument(GraphSnapshotDocument d)
    {
        List<GraphNode> nodes = JsonEntitySerializer.Deserialize<List<GraphNode>>(d.NodesJson);
        List<GraphEdge> edges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(d.EdgesJson);
        List<string> warnings = JsonEntitySerializer.Deserialize<List<string>>(d.WarningsJson);

        return new GraphSnapshot
        {
            SchemaVersion = d.SchemaVersion,
            GraphSnapshotId = Guid.Parse(d.GraphSnapshotId),
            ContextSnapshotId = Guid.Parse(d.ContextSnapshotId),
            RunId = Guid.Parse(d.RunId),
            CreatedUtc = DateTime.Parse(d.CreatedUtc, null, DateTimeStyles.RoundtripKind).ToUniversalTime(),
            Nodes = nodes,
            Edges = edges,
            Warnings = warnings
        };
    }
}
