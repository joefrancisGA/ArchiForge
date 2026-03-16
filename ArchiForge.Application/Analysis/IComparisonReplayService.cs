namespace ArchiForge.Application.Analysis;

public interface IComparisonReplayService
{
    Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the comparison record, rehydrates stored payload, regenerates, and returns drift analysis.</summary>
    Task<DriftAnalysisResult> AnalyzeDriftAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default);
}

