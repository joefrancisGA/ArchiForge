using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

public sealed class ComparisonReplayService : IComparisonReplayService
{
    private readonly IComparisonRecordRepository _comparisonRecordRepository;
    private readonly IEndToEndReplayComparisonSummaryFormatter _endToEndSummaryFormatter;
    private readonly IEndToEndReplayComparisonExportService _endToEndExportService;
    private readonly IExportRecordDiffSummaryFormatter _exportRecordDiffSummaryFormatter;

    public ComparisonReplayService(
        IComparisonRecordRepository comparisonRecordRepository,
        IEndToEndReplayComparisonSummaryFormatter endToEndSummaryFormatter,
        IEndToEndReplayComparisonExportService endToEndExportService,
        IExportRecordDiffSummaryFormatter exportRecordDiffSummaryFormatter)
    {
        _comparisonRecordRepository = comparisonRecordRepository;
        _endToEndSummaryFormatter = endToEndSummaryFormatter;
        _endToEndExportService = endToEndExportService;
        _exportRecordDiffSummaryFormatter = exportRecordDiffSummaryFormatter;
    }

    public async Task<ReplayComparisonResult> ReplayAsync(
        ReplayComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ComparisonRecordId))
        {
            throw new InvalidOperationException("ComparisonRecordId is required.");
        }

        var record = await _comparisonRecordRepository.GetByIdAsync(
            request.ComparisonRecordId,
            cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException(
                $"Comparison record '{request.ComparisonRecordId}' was not found.");
        }

        var format = NormalizeFormat(request.Format);

        return record.ComparisonType switch
        {
            "end-to-end-replay" => await ReplayEndToEndAsync(record, format, cancellationToken),
            "export-record-diff" => await ReplayExportDiffAsync(record, format, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Replay is not supported for comparison type '{record.ComparisonType}'.")
        };
    }

    private async Task<ReplayComparisonResult> ReplayEndToEndAsync(
        ComparisonRecord record,
        string format,
        CancellationToken cancellationToken)
    {
        var report = ComparisonRecordPayloadRehydrator.RehydrateEndToEnd(record)
            ?? throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' did not contain a valid end-to-end payload.");

        if (string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            var markdown = _endToEndExportService.GenerateMarkdown(report);

            return new ReplayComparisonResult
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "markdown",
                FileName = $"comparison_{record.ComparisonRecordId}.md",
                Content = markdown
            };
        }

        if (string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _endToEndExportService.GenerateDocxAsync(
                report,
                cancellationToken);

            return new ReplayComparisonResult
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "docx",
                FileName = $"comparison_{record.ComparisonRecordId}.docx",
                BinaryContent = bytes
            };
        }

        throw new InvalidOperationException($"Unsupported replay format '{format}'.");
    }

    private Task<ReplayComparisonResult> ReplayExportDiffAsync(
        ComparisonRecord record,
        string format,
        CancellationToken cancellationToken)
    {
        var diff = ComparisonRecordPayloadRehydrator.RehydrateExportDiff(record)
            ?? throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' did not contain a valid export-diff payload.");

        if (!string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Export-record diff replay currently supports markdown only.");
        }

        var markdown = _exportRecordDiffSummaryFormatter.FormatMarkdown(diff);

        return Task.FromResult(new ReplayComparisonResult
        {
            ComparisonRecordId = record.ComparisonRecordId,
            ComparisonType = record.ComparisonType,
            Format = "markdown",
            FileName = $"comparison_{record.ComparisonRecordId}.md",
            Content = markdown
        });
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

