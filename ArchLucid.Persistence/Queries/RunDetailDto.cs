using System.Text.Json.Serialization;

using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Queries;

/// <summary>
/// Aggregated read model for a single run: core <see cref="RunRecord"/> plus optional hydrated snapshots and manifest.
/// </summary>
/// <remarks>
/// Returned directly from <c>GET api/authority/runs/{runId}</c> (<c>AuthorityQueryController</c>) as JSON; clients receive embedded domain models for that route.
/// Adding serializable properties on <see cref="RunRecord"/> changes the OpenAPI schema; refresh
/// <c>ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json</c> with
/// <c>ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractSnapshotTests</c> (see <c>OpenApiContractSnapshotTests</c>).
/// </remarks>
public class RunDetailDto
{
    /// <summary>Canonical run row (ids, metadata).</summary>
    public RunRecord Run { get; set; } = null!;

    /// <summary>Context payload when <see cref="RunRecord.ContextSnapshotId"/> resolves.</summary>
    public ContextSnapshot? ContextSnapshot { get; set; }
    /// <summary>Graph payload when <see cref="RunRecord.GraphSnapshotId"/> resolves.</summary>
    public GraphSnapshot? GraphSnapshot { get; set; }

    /// <summary>Findings payload when <see cref="RunRecord.FindingsSnapshotId"/> resolves.</summary>
    public FindingsSnapshot? FindingsSnapshot { get; set; }

    /// <summary>Authority rule-audit trace when <see cref="RunRecord.DecisionTraceId"/> resolves.</summary>
    [JsonPropertyName("decisionTrace")]
    public DecisionTrace? AuthorityTrace { get; set; }

    /// <summary>Golden manifest when <see cref="RunRecord.GoldenManifestId"/> resolves.</summary>
    public GoldenManifest? GoldenManifest { get; set; }

    /// <summary>Synthesized artifacts when both bundle and manifest ids are present and lookup succeeds.</summary>
    public ArtifactBundle? ArtifactBundle { get; set; }
}
