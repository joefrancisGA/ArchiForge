namespace ArchLucid.ContextIngestion.Interfaces;

/// <summary>
///     Runs connector fetch/normalize (parallel) then delta/summary segments (sequential pipeline order).
/// </summary>
public interface IConnectorPipelineOrchestrator
{
    Task<Models.ConnectorPipelineStagesOutcome> RunStagesAsync(
        Models.ContextIngestionRequest request,
        Models.ContextSnapshot? previousSnapshot,
        CancellationToken ct);
}
