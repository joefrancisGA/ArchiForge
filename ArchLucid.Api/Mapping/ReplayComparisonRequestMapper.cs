using ArchiForge.Api.Models;

using AppReplayComparisonRequest = ArchiForge.Application.Analysis.ReplayComparisonRequest;

namespace ArchiForge.Api.Mapping;

/// <summary>Maps API replay models to Application <see cref="ArchiForge.Application.Analysis.ReplayComparisonRequest"/>.</summary>
internal static class ReplayComparisonRequestMapper
{
    public static AppReplayComparisonRequest ToApplication(string comparisonRecordId, ReplayComparisonRequest request) =>
        new()
        {
            ComparisonRecordId = comparisonRecordId,
            Format = request.Format,
            ReplayMode = request.ReplayMode,
            Profile = request.Profile,
            PersistReplay = request.PersistReplay
        };

    /// <summary>Applies optional <c>format</c> query when the body left format empty, then maps.</summary>
    public static AppReplayComparisonRequest ToApplicationForReplayEndpoint(
        string comparisonRecordId,
        ReplayComparisonRequest request,
        string? formatFromQuery)
    {
        string format = request.Format;
        if (!string.IsNullOrWhiteSpace(formatFromQuery) && string.IsNullOrWhiteSpace(format))
            format = formatFromQuery;

        return new AppReplayComparisonRequest
        {
            ComparisonRecordId = comparisonRecordId,
            Format = format,
            ReplayMode = request.ReplayMode,
            Profile = request.Profile,
            PersistReplay = request.PersistReplay
        };
    }

    /// <summary>Used by GET summary when stored markdown is missing — markdown artifact, no persist.</summary>
    public static AppReplayComparisonRequest ForSummaryMarkdown(string comparisonRecordId) =>
        new()
        {
            ComparisonRecordId = comparisonRecordId,
            Format = "markdown",
            ReplayMode = "artifact",
            PersistReplay = false
        };

    public static AppReplayComparisonRequest ToApplicationForBatchEntry(
        string comparisonRecordId,
        string format,
        string replayMode,
        string? profile,
        bool persistReplay) =>
        new()
        {
            ComparisonRecordId = comparisonRecordId,
            Format = format,
            ReplayMode = replayMode,
            Profile = profile,
            PersistReplay = persistReplay
        };
}
