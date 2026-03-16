namespace ArchiForge.Application.Analysis;

public interface IComparisonReplayService
{
    Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        CancellationToken cancellationToken = default);
}

