using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Copies trace identifiers from merge output into <see cref="ManifestMetadata.DecisionTraceIds" />.
/// </summary>
public static class DecisionTraceManifestAttachment
{
    public static void Attach(GoldenManifest manifest, IReadOnlyCollection<DecisionTrace> traces)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(traces);

        manifest.Metadata.DecisionTraceIds = traces
            .Select(t => t.RequireRunEvent().TraceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
