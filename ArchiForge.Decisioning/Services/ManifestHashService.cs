using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Services;

public sealed class ManifestHashService : IManifestHashService
{
    public string ComputeHash(GoldenManifest manifest)
    {
        var canonical = JsonSerializer.Serialize(new
        {
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
                    SupportingFindingIds = d.SupportingFindingIds.OrderBy(x => x).ToArray()
                })
                .ToArray(),
            Assumptions = manifest.Assumptions.OrderBy(x => x).ToArray(),
            Warnings = manifest.Warnings.OrderBy(x => x).ToArray(),
            manifest.Provenance
        });

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
