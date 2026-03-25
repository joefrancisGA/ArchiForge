using System.Diagnostics;

using ArchiForge.Application;
using ArchiForge.Application.Analysis;

namespace ArchiForge.Api.Services;

/// <summary>
/// API-layer wrapper around <see cref="IComparisonReplayService"/> that adds timing,
/// structured diagnostics recording, and structured log emission for each replay attempt.
/// </summary>
/// <param name="inner">Core replay service that performs the actual comparison reconstruction.</param>
/// <param name="replayDiagnosticsRecorder">In-memory ring-buffer that stores per-attempt diagnostics.</param>
/// <param name="logger">Structured logger for success and warning entries.</param>
/// <remarks>
/// On success, a <see cref="ReplayDiagnosticsEntry"/> with <c>Success = true</c> is recorded and an
/// <c>Information</c> log entry is emitted. On <see cref="InvalidOperationException"/> or
/// <see cref="RunNotFoundException"/>, a failure entry is recorded, a <c>Warning</c> is logged,
/// and the exception is rethrown so the calling controller can map it to the appropriate HTTP status.
/// All other exceptions propagate without being recorded.
/// </remarks>
public sealed class ComparisonReplayApiService(
    IComparisonReplayService inner,
    IReplayDiagnosticsRecorder replayDiagnosticsRecorder,
    ILogger<ComparisonReplayApiService> logger)
    : IComparisonReplayApiService
{
    /// <summary>
    /// Executes a comparison replay, records diagnostics, and logs the outcome.
    /// </summary>
    /// <param name="request">Replay configuration including comparison record id, format, mode, and persist flag.</param>
    /// <param name="metadataOnly">When <see langword="true"/>, only metadata is returned; artifact re-generation is skipped.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The full replay result from the inner service.</returns>
    /// <exception cref="InvalidOperationException">Rethrown from <paramref name="inner"/> when the comparison record is invalid.</exception>
    /// <exception cref="RunNotFoundException">Rethrown from <paramref name="inner"/> when a referenced run cannot be found.</exception>
    public async Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        bool metadataOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await inner.ReplayAsync(request, cancellationToken);
            sw.Stop();

            replayDiagnosticsRecorder.Record(new ReplayDiagnosticsEntry
            {
                TimestampUtc = DateTime.UtcNow,
                ComparisonRecordId = request.ComparisonRecordId,
                ComparisonType = result.ComparisonType,
                Format = result.Format,
                ReplayMode = result.ReplayMode,
                PersistReplay = request.PersistReplay,
                DurationMs = sw.ElapsedMilliseconds,
                Success = true,
                VerificationPassed = result.VerificationPassed,
                PersistedReplayRecordId = result.PersistedReplayRecordId,
                MetadataOnly = metadataOnly
            });

            logger.LogInformation(
                "Comparison replay: ComparisonRecordId={ComparisonRecordId}, Type={ComparisonType}, Format={Format}, ReplayMode={ReplayMode}, PersistReplay={PersistReplay}, MetadataOnly={MetadataOnly}, DurationMs={DurationMs}, VerificationPassed={VerificationPassed}",
                request.ComparisonRecordId,
                result.ComparisonType,
                result.Format,
                result.ReplayMode,
                request.PersistReplay,
                metadataOnly,
                sw.ElapsedMilliseconds,
                result.VerificationPassed);

            return result;
        }
        catch (Exception ex) when (ex is InvalidOperationException or RunNotFoundException)
        {
            sw.Stop();

            replayDiagnosticsRecorder.Record(new ReplayDiagnosticsEntry
            {
                TimestampUtc = DateTime.UtcNow,
                ComparisonRecordId = request.ComparisonRecordId,
                ComparisonType = string.Empty,
                Format = request.Format,
                ReplayMode = request.ReplayMode,
                PersistReplay = request.PersistReplay,
                DurationMs = sw.ElapsedMilliseconds,
                Success = false,
                ErrorMessage = ex.Message,
                MetadataOnly = metadataOnly
            });

            var notFound = ex is RunNotFoundException;
            logger.LogWarning(
                ex,
                "Comparison replay failed: ComparisonRecordId={ComparisonRecordId}, NotFound={NotFound}, MetadataOnly={MetadataOnly}, Error={Error}",
                request.ComparisonRecordId,
                notFound,
                metadataOnly,
                ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Delegates drift analysis for <paramref name="comparisonRecordId"/> directly to the inner service.
    /// </summary>
    /// <param name="comparisonRecordId">Identifier of the comparison record to analyse.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The drift analysis result produced by the inner service.</returns>
    public Task<DriftAnalysisResult> AnalyzeDriftAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default)
    {
        return inner.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
    }
}

