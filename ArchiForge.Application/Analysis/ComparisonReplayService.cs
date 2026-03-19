using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Replays persisted comparison records into exportable artifacts (Markdown/HTML/DOCX/PDF),
/// without requiring users to manually rebuild the comparison.
/// </summary>
/// <remarks>
/// This service supports three replay modes:
/// - <c>artifact</c>: export the stored payload as-is (fastest; does not require source runs/exports to exist)
/// - <c>regenerate</c>: rebuild the comparison from source data (requires the referenced runs/exports to exist)
/// - <c>verify</c>: regenerate and compare against stored payload, returning drift analysis
/// </remarks>
public sealed class ComparisonReplayService(
    IComparisonRecordRepository comparisonRecordRepository,
    IComparisonAuditService comparisonAuditService,
    IComparisonDriftAnalyzer driftAnalyzer,
    IEndToEndReplayComparisonService endToEndReplayComparisonService,
    IEndToEndReplayComparisonExportService endToEndExportService,
    IExportRecordDiffService exportRecordDiffService,
    IExportRecordDiffSummaryFormatter exportRecordDiffSummaryFormatter,
    IExportRecordDiffExportService exportRecordDiffExportService,
    IRunExportRecordRepository runExportRecordRepository)
    : IComparisonReplayService
{
    /// <summary>
    /// Replay a comparison record by ID and return an export payload (text or binary).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the record does not exist, its payload cannot be rehydrated, or the requested
    /// format/mode is not supported for the record type.
    /// </exception>
    public async Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ComparisonRecordId))
        {
            throw new InvalidOperationException("ComparisonRecordId is required.");
        }

        var record = await comparisonRecordRepository.GetByIdAsync(
            request.ComparisonRecordId,
            cancellationToken) ?? throw new InvalidOperationException(
                $"Comparison record '{request.ComparisonRecordId}' was not found.");
        var format = NormalizeFormat(request.Format);
        var profile = EndToEndComparisonExportProfile.Normalize(request.Profile);
        var mode = ParseReplayMode(request.ReplayMode);

        var result = record.ComparisonType switch
        {
            "end-to-end-replay" => await ReplayEndToEndAsync(record, format, profile, mode, cancellationToken),
            "export-record-diff" => await ReplayExportDiffAsync(record, format, mode, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Replay is not supported for comparison type '{record.ComparisonType}'.")
        };

        if (request.PersistReplay)
        {
            // Intentionally persists a *new* comparison record rather than mutating the original.
            // This keeps comparison records immutable and yields an audit trail of replay activity.
            result.PersistedReplayRecordId = await comparisonAuditService.RecordReplayOfAsync(
                record,
                notes: $"Replay of comparison record {record.ComparisonRecordId} at {DateTime.UtcNow:O}.",
                cancellationToken);
        }

        return result;
    }

    public async Task<DriftAnalysisResult> AnalyzeDriftAsync(
        string comparisonRecordId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(comparisonRecordId))
        {
            throw new InvalidOperationException("ComparisonRecordId is required.");
        }

        var record = await comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken) ?? throw new InvalidOperationException(
                $"Comparison record '{comparisonRecordId}' was not found.");
        return record.ComparisonType switch
        {
            "end-to-end-replay" => await AnalyzeDriftEndToEndAsync(record, cancellationToken),
            "export-record-diff" => await AnalyzeDriftExportDiffAsync(record, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Drift analysis is not supported for comparison type '{record.ComparisonType}'.")
        };
    }

    private async Task<DriftAnalysisResult> AnalyzeDriftEndToEndAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken)
    {
        var stored = ComparisonRecordPayloadRehydrator.RehydrateEndToEnd(record)
            ?? throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' did not contain a valid end-to-end payload.");
        var regenerated = await RegenerateEndToEndAsync(record, cancellationToken);
        return driftAnalyzer.Analyze(stored, regenerated);
    }

    private async Task<DriftAnalysisResult> AnalyzeDriftExportDiffAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken)
    {
        var stored = ComparisonRecordPayloadRehydrator.RehydrateExportDiff(record)
            ?? throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' did not contain a valid export-diff payload.");
        var regenerated = await RegenerateExportDiffAsync(record, cancellationToken);
        return driftAnalyzer.Analyze(stored, regenerated);
    }

    private async Task<ReplayComparisonResult> ReplayEndToEndAsync(
        ComparisonRecord record,
        string format,
        string profile,
        ComparisonReplayMode mode,
        CancellationToken cancellationToken)
    {
        EndToEndReplayComparisonReport report;

        switch (mode)
        {
            case ComparisonReplayMode.ArtifactReplay:
                report = ComparisonRecordPayloadRehydrator.RehydrateEndToEnd(record)
                    ?? throw new InvalidOperationException(
                        $"Comparison record '{record.ComparisonRecordId}' did not contain a valid end-to-end payload.");
                break;
            case ComparisonReplayMode.Regenerate:
                report = await RegenerateEndToEndAsync(record, cancellationToken);
                break;
            case ComparisonReplayMode.Verify:
                var storedE2E = ComparisonRecordPayloadRehydrator.RehydrateEndToEnd(record)
                    ?? throw new InvalidOperationException(
                        $"Comparison record '{record.ComparisonRecordId}' did not contain a valid end-to-end payload.");
                report = await RegenerateEndToEndAsync(record, cancellationToken);
                var driftE2E = driftAnalyzer.Analyze(storedE2E, report);
                if (driftE2E.DriftDetected)
                {
                    throw new ComparisonVerificationFailedException(
                        driftE2E.Summary,
                        driftE2E);
                }
                break;
            default:
                throw new InvalidOperationException($"Unsupported replay mode '{mode}'.");
        }

        var result = await BuildEndToEndResultAsync(record, report, format, profile, cancellationToken);
        result.ReplayMode = FormatReplayMode(mode);
        if (mode == ComparisonReplayMode.Verify)
        {
            result.VerificationPassed = true;
            result.VerificationMessage = "Regenerated comparison matches stored payload.";
        }
        return result;
    }

    private async Task<EndToEndReplayComparisonReport> RegenerateEndToEndAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.LeftRunId) || string.IsNullOrWhiteSpace(record.RightRunId))
        {
            throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' has no LeftRunId/RightRunId; cannot regenerate end-to-end comparison.");
        }

        return await endToEndReplayComparisonService.BuildAsync(
            record.LeftRunId,
            record.RightRunId,
            cancellationToken);
    }

    private async Task<ReplayComparisonResult> BuildEndToEndResultAsync(
        ComparisonRecord record,
        EndToEndReplayComparisonReport report,
        string format,
        string profile,
        CancellationToken cancellationToken)
    {
        if (string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            var markdown = endToEndExportService.GenerateMarkdown(report, profile);
            var r = new ReplayComparisonResult
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "markdown",
                FileName = $"comparison_{record.ComparisonRecordId}.md",
                Content = markdown
            };
            SetRecordMetadata(r, record, profile);
            return r;
        }

        if (string.Equals(format, "html", StringComparison.OrdinalIgnoreCase))
        {
            var html = endToEndExportService.GenerateHtml(report, profile);
            var r = new ReplayComparisonResult
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "html",
                FileName = $"comparison_{record.ComparisonRecordId}.html",
                Content = html
            };
            SetRecordMetadata(r, record, profile);
            return r;
        }

        if (string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await endToEndExportService.GenerateDocxAsync(
                report,
                cancellationToken,
                profile);
            var r = new ReplayComparisonResult
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "docx",
                FileName = $"comparison_{record.ComparisonRecordId}.docx",
                BinaryContent = bytes
            };
            SetRecordMetadata(r, record, profile);
            return r;
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await endToEndExportService.GeneratePdfAsync(
                report,
                cancellationToken,
                profile);
            var r = new ReplayComparisonResult
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "pdf",
                FileName = $"comparison_{record.ComparisonRecordId}.pdf",
                BinaryContent = bytes
            };
            SetRecordMetadata(r, record, profile);
            return r;
        }

        throw new InvalidOperationException($"Unsupported replay format '{format}'.");
    }

    private async Task<ReplayComparisonResult> ReplayExportDiffAsync(
        ComparisonRecord record,
        string format,
        ComparisonReplayMode mode,
        CancellationToken cancellationToken)
    {
        ExportRecordDiffResult diff;

        switch (mode)
        {
            case ComparisonReplayMode.ArtifactReplay:
                diff = ComparisonRecordPayloadRehydrator.RehydrateExportDiff(record)
                    ?? throw new InvalidOperationException(
                        $"Comparison record '{record.ComparisonRecordId}' did not contain a valid export-diff payload.");
                break;
            case ComparisonReplayMode.Regenerate:
                diff = await RegenerateExportDiffAsync(record, cancellationToken);
                break;
            case ComparisonReplayMode.Verify:
                var storedDiff = ComparisonRecordPayloadRehydrator.RehydrateExportDiff(record)
                    ?? throw new InvalidOperationException(
                        $"Comparison record '{record.ComparisonRecordId}' did not contain a valid export-diff payload.");
                diff = await RegenerateExportDiffAsync(record, cancellationToken);
                var driftExport = driftAnalyzer.Analyze(storedDiff, diff);
                if (driftExport.DriftDetected)
                {
                    throw new ComparisonVerificationFailedException(
                        driftExport.Summary,
                        driftExport);
                }
                break;
            default:
                throw new InvalidOperationException($"Unsupported replay mode '{mode}'.");
        }

        if (!string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await exportRecordDiffExportService.GenerateDocxAsync(diff, cancellationToken);
                var resultDocx = new ReplayComparisonResult
                {
                    ComparisonRecordId = record.ComparisonRecordId,
                    ComparisonType = record.ComparisonType,
                    Format = "docx",
                    FileName = $"comparison_{record.ComparisonRecordId}.docx",
                    BinaryContent = bytes,
                    ReplayMode = FormatReplayMode(mode)
                };
                if (mode == ComparisonReplayMode.Verify)
                {
                    resultDocx.VerificationPassed = true;
                    resultDocx.VerificationMessage = "Regenerated comparison matches stored payload.";
                }
                SetRecordMetadata(resultDocx, record, formatProfile: null);
                return resultDocx;
            }

            throw new InvalidOperationException($"Unsupported replay format '{format}' for export-record diff.");
        }

        var markdown = exportRecordDiffSummaryFormatter.FormatMarkdown(diff);
        var result = new ReplayComparisonResult
        {
            ComparisonRecordId = record.ComparisonRecordId,
            ComparisonType = record.ComparisonType,
            Format = "markdown",
            FileName = $"comparison_{record.ComparisonRecordId}.md",
            Content = markdown,
            ReplayMode = FormatReplayMode(mode)
        };
        SetRecordMetadata(result, record, null);
        if (mode == ComparisonReplayMode.Verify)
        {
            result.VerificationPassed = true;
            result.VerificationMessage = "Regenerated comparison matches stored payload.";
        }
        SetRecordMetadata(result, record, formatProfile: null);
        return result;
    }

    private static void SetRecordMetadata(ReplayComparisonResult r, ComparisonRecord record, string? formatProfile)
    {
        r.LeftRunId = record.LeftRunId;
        r.RightRunId = record.RightRunId;
        r.LeftExportRecordId = record.LeftExportRecordId;
        r.RightExportRecordId = record.RightExportRecordId;
        r.CreatedUtc = record.CreatedUtc;
        r.FormatProfile = formatProfile;
    }

    private async Task<ExportRecordDiffResult> RegenerateExportDiffAsync(
        ComparisonRecord record,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.LeftExportRecordId) || string.IsNullOrWhiteSpace(record.RightExportRecordId))
        {
            throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' has no LeftExportRecordId/RightExportRecordId; cannot regenerate export-record diff.");
        }

        var left = await runExportRecordRepository.GetByIdAsync(record.LeftExportRecordId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Export record '{record.LeftExportRecordId}' was not found.");
        var right = await runExportRecordRepository.GetByIdAsync(record.RightExportRecordId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Export record '{record.RightExportRecordId}' was not found.");

        return exportRecordDiffService.Compare(left, right);
    }

    private static ComparisonReplayMode ParseReplayMode(string? replayMode)
    {
        var value = (replayMode ?? "artifact").Trim().ToLowerInvariant();
        return value switch
        {
            "artifact" => ComparisonReplayMode.ArtifactReplay,
            "regenerate" => ComparisonReplayMode.Regenerate,
            "verify" => ComparisonReplayMode.Verify,
            _ => ComparisonReplayMode.ArtifactReplay
        };
    }

    private static string FormatReplayMode(ComparisonReplayMode mode)
    {
        return mode switch
        {
            ComparisonReplayMode.ArtifactReplay => "artifact",
            ComparisonReplayMode.Regenerate => "regenerate",
            ComparisonReplayMode.Verify => "verify",
            _ => "artifact"
        };
    }

    private static string NormalizeFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return "markdown";
        }

        return format.Trim().ToLowerInvariant();
    }
}

