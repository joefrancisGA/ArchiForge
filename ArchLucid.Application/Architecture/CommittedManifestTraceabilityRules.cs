using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Architecture;

/// <summary>
/// Integrity rules for committed coordinator runs: golden manifest metadata must list every persisted
/// <see cref="DecisionTrace"/> id (populated by <c>AttachDecisionTraceIds</c> during merge).
/// </summary>
public static class CommittedManifestTraceabilityRules
{
    /// <summary>
    /// Returns human-readable gaps when <paramref name="detail"/> is committed but manifest trace ids are inconsistent.
    /// </summary>
    public static IReadOnlyList<string> GetLinkageGaps(ArchitectureRunDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        if (detail.Run.Status != ArchitectureRunStatus.Committed)
            return [];

        return GetLinkageGaps(detail.Manifest, detail.DecisionTraces);
    }

    /// <summary>
    /// Validates manifest <see cref="ManifestMetadata.DecisionTraceIds"/> against coordinator <see cref="DecisionTrace"/> rows.
    /// </summary>
    public static IReadOnlyList<string> GetLinkageGaps(
        GoldenManifest? manifest,
        IReadOnlyList<DecisionTrace> traces)
    {
        if (manifest is null)
            return [];

        List<string> gaps = [];
        HashSet<string> idsOnManifest = new(StringComparer.OrdinalIgnoreCase);

        if (manifest.Metadata.DecisionTraceIds is not null)
        {
            foreach (string id in manifest.Metadata.DecisionTraceIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    gaps.Add("Manifest.Metadata.DecisionTraceIds contains an empty entry.");
                else
                    idsOnManifest.Add(id);
            }
        }

        List<string> coordinatorTraceIds = [];

        foreach (DecisionTrace trace in traces)
        {
            if (trace is not RunEventTrace runEventTrace)
            {
                gaps.Add(
                    $"Decision trace row is not a {nameof(RunEventTrace)} (Kind={trace.Kind}); coordinator commits expect run-event traces only.");

                continue;
            }

            string traceId = runEventTrace.RunEvent.TraceId;

            if (string.IsNullOrWhiteSpace(traceId))
                gaps.Add("A coordinator decision trace has an empty TraceId.");
            else
                coordinatorTraceIds.Add(traceId);
        }

        HashSet<string> coordinatorSet = new(coordinatorTraceIds, StringComparer.OrdinalIgnoreCase);

        foreach (string tid in coordinatorSet)
        {
            if (!idsOnManifest.Contains(tid))
                gaps.Add($"Trace '{tid}' is missing from Manifest.Metadata.DecisionTraceIds.");
        }

        foreach (string mid in idsOnManifest)
        {
            if (!coordinatorSet.Contains(mid))
                gaps.Add($"Manifest.Metadata.DecisionTraceIds lists unknown trace id '{mid}'.");
        }

        return gaps;
    }
}
