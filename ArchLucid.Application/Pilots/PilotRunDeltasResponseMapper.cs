using ArchLucid.Contracts.Pilots;

namespace ArchLucid.Application.Pilots;

/// <summary>Maps <see cref="PilotRunDeltas" /> to the HTTP JSON contract <see cref="PilotRunDeltasResponse" />.</summary>
public static class PilotRunDeltasResponseMapper
{
    public static PilotRunDeltasResponse ToResponse(PilotRunDeltas deltas)
    {
        return new PilotRunDeltasResponse
        {
            TimeToCommittedManifestTotalSeconds = deltas.TimeToCommittedManifest?.TotalSeconds,
            ManifestCommittedUtc = deltas.ManifestCommittedUtc,
            RunCreatedUtc = deltas.RunCreatedUtc,
            FindingsBySeverity = deltas.FindingsBySeverity
                .Select(p => new PilotRunDeltaSeverityCountResponse { Severity = p.Key, Count = p.Value })
                .ToList(),
            AuditRowCount = deltas.AuditRowCount,
            AuditRowCountTruncated = deltas.AuditRowCountTruncated,
            LlmCallCount = deltas.LlmCallCount,
            TopFindingSeverity = deltas.TopFindingSeverity,
            TopFindingId = deltas.TopFindingId,
            TopFindingEvidenceChain = deltas.TopFindingEvidenceChain,
            IsDemoTenant = deltas.IsDemoTenant
        };
    }
}
