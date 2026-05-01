namespace ArchLucid.Application.Analysis;

/// <summary>
///     Shared parsing for replay query parameters (<see cref="ReplayComparisonRequest" />) used by
///     <see cref="ComparisonReplayService" /> and <see cref="ComparisonReplayCostEstimator" />.
/// </summary>
public static class ComparisonReplayRequestParsing
{
    /// <summary>Normalizes format; default is markdown when null or whitespace.</summary>
    public static string NormalizeFormat(string? format)
    {
        return string.IsNullOrWhiteSpace(format) ? "markdown" : format.Trim().ToLowerInvariant();
    }

    /// <summary>
    ///     Parses replay mode: artifact (default), regenerate, verify.
    /// </summary>
    /// <exception cref="ArgumentException">When <paramref name="replayMode" /> is not a supported token.</exception>
    public static ComparisonReplayMode ParseReplayMode(string? replayMode)
    {
        string value = (replayMode ?? "artifact").Trim().ToLowerInvariant();

        return value switch
        {
            "artifact" => ComparisonReplayMode.ArtifactReplay,
            "regenerate" => ComparisonReplayMode.Regenerate,
            "verify" => ComparisonReplayMode.Verify,
            _ => throw new ArgumentException(
                $"Unknown replay mode '{replayMode}'. Supported modes: artifact, regenerate, verify.",
                nameof(replayMode))
        };
    }

    /// <summary>Inverse of <see cref="ParseReplayMode" /> for API responses.</summary>
    public static string FormatReplayMode(ComparisonReplayMode mode)
    {
        return mode switch
        {
            ComparisonReplayMode.ArtifactReplay => "artifact",
            ComparisonReplayMode.Regenerate => "regenerate",
            ComparisonReplayMode.Verify => "verify",
            _ => "artifact"
        };
    }
}
