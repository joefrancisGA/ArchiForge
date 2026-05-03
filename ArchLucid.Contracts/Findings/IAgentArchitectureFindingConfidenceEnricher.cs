namespace ArchLucid.Contracts.Findings;

/// <summary>
///     Best-effort enrichment of simulator/API agent findings after evaluation hooks run (never blocks callers).
/// </summary>
public interface IAgentArchitectureFindingConfidenceEnricher
{
    Task TryEnrichRunAsync(string runId, CancellationToken cancellationToken);
}
