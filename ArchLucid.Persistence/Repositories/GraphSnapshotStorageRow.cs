using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// Dapper row shape for <c>dbo.GraphSnapshots</c> JSON columns before mapping to <see cref="ArchiForge.KnowledgeGraph.Models.GraphSnapshot"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
public sealed class GraphSnapshotStorageRow
{
    public Guid GraphSnapshotId { get; init; }
    public Guid ContextSnapshotId { get; init; }
    public Guid RunId { get; init; }
    public DateTime CreatedUtc { get; init; }
    public string NodesJson { get; init; } = null!;
    public string EdgesJson { get; init; } = null!;
    public string WarningsJson { get; init; } = null!;
}
