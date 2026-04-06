namespace ArchiForge.Application.Analysis;

/// <summary>
/// Builds a comprehensive <see cref="ArchitectureAnalysisReport"/> for a given run.
/// </summary>
public interface IArchitectureAnalysisService
{
    /// <summary>
    /// Assembles an analysis report for <paramref name="request"/>, optionally including
    /// evidence, execution traces, manifest, diagram, summary, determinism check, and diffs.
    /// </summary>
    /// <param name="request">Analysis options; <see cref="ArchitectureAnalysisRequest.RunId"/> is required.</param>
    /// <param name="cancellationToken">Propagates cancellation to all async operations.</param>
    /// <returns>The populated <see cref="ArchitectureAnalysisReport"/>.</returns>
    /// <exception cref="RunNotFoundException">Thrown when the specified run does not exist.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <see cref="ArchitectureAnalysisRequest.PreloadedRunDetail"/> does not match <see cref="ArchitectureAnalysisRequest.RunId"/>.</exception>
    Task<ArchitectureAnalysisReport> BuildAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
