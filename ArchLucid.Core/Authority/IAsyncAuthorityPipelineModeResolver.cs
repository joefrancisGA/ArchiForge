namespace ArchLucid.Core.Authority;

/// <summary>
///     Decides whether the authority pipeline should queue context ingestion and graph stages for asynchronous processing.
/// </summary>
public interface IAsyncAuthorityPipelineModeResolver
{
    /// <summary>
    ///     When true, the authority run orchestrator may persist only the run header and enqueue continuation work
    ///     instead of completing context ingestion and graph resolution inline.
    /// </summary>
    Task<bool> ShouldQueueContextAndGraphStagesAsync(CancellationToken cancellationToken = default);
}
