using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Services;

/// <summary>
/// Computes a deterministic SHA-256 hash over a canonical JSON projection of a <see cref="GoldenManifest"/>.
/// </summary>
/// <remarks>
/// The canonical projection includes all structural manifest fields (topology, decisions, requirements,
/// security, compliance, cost, constraints, provenance) but excludes non-deterministic metadata like
/// <c>CreatedUtc</c>. Collection entries are sorted before serialization so that insertion-order
/// differences do not produce different hashes.
/// </remarks>
public sealed class ManifestHashService : IManifestHashService
{
    /// <inheritdoc />
    public string ComputeHash(GoldenManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        string canonical = JsonSerializer.Serialize(new
        {
            manifest.TenantId,
            manifest.WorkspaceId,
            manifest.ProjectId,
            manifest.ManifestId,
            manifest.RunId,
            manifest.ContextSnapshotId,
            manifest.GraphSnapshotId,
            manifest.FindingsSnapshotId,
            manifest.DecisionTraceId,
            manifest.RuleSetId,
            manifest.RuleSetVersion,
            manifest.RuleSetHash,
            manifest.Metadata,
            manifest.Requirements,
            manifest.Topology,
            manifest.Security,
            manifest.Compliance,
            manifest.Cost,
            manifest.Constraints,
            manifest.UnresolvedIssues,
            Decisions = manifest.Decisions
                .OrderBy(d => d.DecisionId)
                .Select(d => new
                {
                    d.DecisionId,
                    d.Category,
                    d.Title,
                    d.SelectedOption,
                    d.Rationale,
                    SupportingFindingIds = d.SupportingFindingIds.OrderBy(x => x).ToArray(),
                    RelatedNodeIds = d.RelatedNodeIds.OrderBy(x => x).ToArray(),
                    d.RawDecisionJson,
                })
                .ToArray(),
            Assumptions = manifest.Assumptions.OrderBy(x => x).ToArray(),
            Warnings = manifest.Warnings.OrderBy(x => x).ToArray(),
            manifest.Policy,
            manifest.Provenance
        });

        using SHA256 sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(canonical);
        byte[] hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
