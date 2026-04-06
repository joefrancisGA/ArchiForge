using ArchiForge.Contracts.Metadata;
using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Application.Analysis;

/// <inheritdoc cref="IComparisonReplayCostEstimator" />
public sealed class ComparisonReplayCostEstimator(IComparisonRecordRepository comparisonRecords)
    : IComparisonReplayCostEstimator
{
    private readonly IComparisonRecordRepository _comparisonRecords =
        comparisonRecords ?? throw new ArgumentNullException(nameof(comparisonRecords));

    /// <inheritdoc />
    public async Task<ComparisonReplayCostEstimate?> TryEstimateAsync(
        string comparisonRecordId,
        string? format,
        string? replayMode,
        bool persistReplay,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonRecordId);

        ComparisonRecord? record = await _comparisonRecords.GetByIdAsync(comparisonRecordId, ct);

        if (record is null)
            return null;

        string normalizedFormat = ComparisonReplayRequestParsing.NormalizeFormat(format);
        ComparisonReplayMode mode = ComparisonReplayRequestParsing.ParseReplayMode(replayMode);
        List<string> factors = [];

        int score = ScoreForRecord(record, normalizedFormat, mode, factors);

        if (persistReplay)
        {
            score += 2;
            factors.Add("PersistReplay adds a new comparison record write.");
        }

        int payloadChars = record.PayloadJson.Length;

        if (payloadChars > 500_000)
        {
            score += 3;
            factors.Add("Large stored payload: expect higher deserialization and formatting cost.");
        }

        int clamped = Math.Clamp(score, 0, 100);
        string band = clamped <= 4 ? "low" : clamped <= 12 ? "medium" : "high";

        return new ComparisonReplayCostEstimate
        {
            ComparisonRecordId = record.ComparisonRecordId,
            ComparisonType = record.ComparisonType,
            Format = normalizedFormat,
            ReplayMode = ComparisonReplayRequestParsing.FormatReplayMode(mode),
            PersistReplay = persistReplay,
            ApproximateRelativeScore = clamped,
            RelativeCostBand = band,
            Factors = factors
        };
    }

    private static int ScoreForRecord(
        ComparisonRecord record,
        string normalizedFormat,
        ComparisonReplayMode mode,
        List<string> factors)
    {
        if (string.Equals(record.ComparisonType, ComparisonTypes.EndToEndReplay, StringComparison.OrdinalIgnoreCase))
            return ScoreEndToEnd(normalizedFormat, mode, factors);

        if (string.Equals(record.ComparisonType, ComparisonTypes.ExportRecordDiff, StringComparison.OrdinalIgnoreCase))
            return ScoreExportDiff(normalizedFormat, mode, factors);

        factors.Add($"Comparison type '{record.ComparisonType}' is not replayable via the standard replay API.");
        return 25;
    }

    private static int ScoreEndToEnd(string normalizedFormat, ComparisonReplayMode mode, List<string> factors)
    {
        int formatWeight = EndToEndFormatWeight(normalizedFormat, factors);
        int modeBase = mode switch
        {
            ComparisonReplayMode.ArtifactReplay => 1,
            ComparisonReplayMode.Regenerate => 6,
            ComparisonReplayMode.Verify => 11,
            _ => 1
        };

        factors.Add(mode switch
        {
            ComparisonReplayMode.ArtifactReplay => "Artifact mode rehydrates the stored payload only.",
            ComparisonReplayMode.Regenerate => "Regenerate rebuilds the comparison from source runs (more SQL + CPU).",
            ComparisonReplayMode.Verify => "Verify regenerates and compares against the stored payload (highest cost).",
            _ => "Replay mode factor applied."
        });

        return modeBase + formatWeight;
    }

    private static int ScoreExportDiff(string normalizedFormat, ComparisonReplayMode mode, List<string> factors)
    {
        int formatWeight = ExportDiffFormatWeight(normalizedFormat, factors);
        int modeBase = mode switch
        {
            ComparisonReplayMode.ArtifactReplay => 2,
            ComparisonReplayMode.Regenerate => 5,
            ComparisonReplayMode.Verify => 9,
            _ => 2
        };

        factors.Add("Export-record diff touches two export rows when regenerating.");

        return modeBase + formatWeight;
    }

    private static int EndToEndFormatWeight(string normalizedFormat, List<string> factors)
    {
        if (string.Equals(normalizedFormat, "markdown", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.Equals(normalizedFormat, "html", StringComparison.OrdinalIgnoreCase))
        {
            factors.Add("HTML formatting adds rendering work versus plain markdown.");
            return 1;
        }

        if (string.Equals(normalizedFormat, "docx", StringComparison.OrdinalIgnoreCase))
        {
            factors.Add("DOCX generation is significantly more expensive than markdown.");
            return 3;
        }

        if (string.Equals(normalizedFormat, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            factors.Add("PDF generation is typically the most expensive text export format.");
            return 4;
        }

        factors.Add($"Format '{normalizedFormat}' is non-standard; replay may fail at execution time.");

        return 2;
    }

    private static int ExportDiffFormatWeight(string normalizedFormat, List<string> factors)
    {
        if (string.Equals(normalizedFormat, "markdown", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.Equals(normalizedFormat, "docx", StringComparison.OrdinalIgnoreCase))
        {
            factors.Add("DOCX export for export diffs uses the document pipeline.");
            return 3;
        }

        factors.Add(
            $"Export-record diff replays support markdown and docx only; '{normalizedFormat}' would be rejected on replay.");

        return 2;
    }
}
