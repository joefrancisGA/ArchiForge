using System.IO.Compression;
using System.Text;
using System.Text.Json;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;

/// <inheritdoc cref="IReferenceEvidenceAdminExportService" />
public sealed class ReferenceEvidenceAdminExportService(
    IReferenceEvidenceRunLookup runLookup,
    IRunDetailQueryService runDetailQuery,
    IPilotRunDeltaComputer deltaComputer,
    FirstValueReportBuilder firstValueReportBuilder,
    FirstValueReportPdfBuilder firstValueReportPdfBuilder,
    SponsorOnePagerPdfBuilder sponsorOnePagerPdfBuilder,
    ILogger<ReferenceEvidenceAdminExportService> logger) : IReferenceEvidenceAdminExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly IReferenceEvidenceRunLookup _runLookup =
        runLookup ?? throw new ArgumentNullException(nameof(runLookup));

    private readonly IRunDetailQueryService _runDetailQuery =
        runDetailQuery ?? throw new ArgumentNullException(nameof(runDetailQuery));

    private readonly IPilotRunDeltaComputer _deltaComputer =
        deltaComputer ?? throw new ArgumentNullException(nameof(deltaComputer));

    private readonly FirstValueReportBuilder _firstValueReportBuilder =
        firstValueReportBuilder ?? throw new ArgumentNullException(nameof(firstValueReportBuilder));

    private readonly FirstValueReportPdfBuilder _firstValueReportPdfBuilder =
        firstValueReportPdfBuilder ?? throw new ArgumentNullException(nameof(firstValueReportPdfBuilder));

    private readonly SponsorOnePagerPdfBuilder _sponsorOnePagerPdfBuilder =
        sponsorOnePagerPdfBuilder ?? throw new ArgumentNullException(nameof(sponsorOnePagerPdfBuilder));

    private readonly ILogger<ReferenceEvidenceAdminExportService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<byte[]?> BuildZipAsync(
        Guid tenantId,
        bool includeDemo,
        string apiBaseForLinks,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Persistence.Models.ReferenceEvidenceRunCandidate> candidates =
            await _runLookup.ListRecentCommittedRunsAsync(tenantId, 200, cancellationToken);

        Persistence.Models.ReferenceEvidenceRunCandidate? selected = null;

        foreach (Persistence.Models.ReferenceEvidenceRunCandidate row in candidates)
        {
            string runKey = row.RunId.ToString("N");

            if (includeDemo
                || (!ContosoRetailDemoIdentifiers.IsDemoRunId(runKey)
                    && !ContosoRetailDemoIdentifiers.IsDemoRequestId(row.RequestId)))
            {
                selected = row;

                break;
            }
        }

        if (selected is null)
            return null;

        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = selected.WorkspaceId,
            ProjectId = selected.ScopeProjectId,
        };

        string runId = selected.RunId.ToString("N");
        string baseUrl = string.IsNullOrWhiteSpace(apiBaseForLinks)
            ? "http://localhost:5000"
            : apiBaseForLinks.Trim().TrimEnd('/');

        using MemoryStream zipStream = new();
        using (ZipArchive zip = new(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (IDisposable _ = AmbientScopeContext.Push(scope))
            {
                if (await _runDetailQuery.GetRunDetailAsync(runId, cancellationToken) is not { } detail)
                    return null;

                PilotRunDeltas deltas = await _deltaComputer.ComputeAsync(detail, cancellationToken);
                PilotRunDeltasResponse deltaDto = PilotRunDeltasResponseMapper.ToResponse(deltas);
                byte[] deltasJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(deltaDto, JsonOptions));

                ZipArchiveEntry deltasEntry = zip.CreateEntry("pilot-run-deltas.json");
                await using (Stream s = deltasEntry.Open())
                {
                    await s.WriteAsync(deltasJson, cancellationToken);
                }

                string? markdown = await _firstValueReportBuilder.BuildMarkdownAsync(runId, baseUrl, cancellationToken);

                if (markdown is not null)
                {
                    ZipArchiveEntry mdEntry = zip.CreateEntry("first-value-report.md");
                    await using (Stream s = mdEntry.Open())
                    {
                        byte[] mdBytes = Encoding.UTF8.GetBytes(markdown);
                        await s.WriteAsync(mdBytes, cancellationToken);
                    }
                }

                try
                {
                    byte[]? firstPdf = await _firstValueReportPdfBuilder.BuildPdfAsync(runId, baseUrl, cancellationToken);

                    if (firstPdf is { Length: > 0 })
                    {
                        ZipArchiveEntry pdfEntry = zip.CreateEntry("first-value-report.pdf");
                        await using (Stream s = pdfEntry.Open())
                        {
                            await s.WriteAsync(firstPdf, cancellationToken);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reference evidence: first-value PDF omitted for run {RunId}.", runId);
                }

                try
                {
                    byte[]? sponsorPdf = await _sponsorOnePagerPdfBuilder.BuildPdfAsync(runId, baseUrl, cancellationToken);

                    if (sponsorPdf is { Length: > 0 })
                    {
                        ZipArchiveEntry spEntry = zip.CreateEntry("sponsor-one-pager.pdf");
                        await using (Stream s = spEntry.Open())
                        {
                            await s.WriteAsync(sponsorPdf, cancellationToken);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reference evidence: sponsor one-pager omitted for run {RunId}.", runId);
                }

                string readme =
                    $"""
                     ArchLucid reference-evidence bundle
                     TenantId: {tenantId:D}
                     RunId: {runId}
                     IncludeDemo: {includeDemo}
                     GeneratedUtc: {DateTime.UtcNow:O}

                     Files:
                     - pilot-run-deltas.json — proof-of-ROI numbers (see PILOT_ROI_MODEL.md).
                     - first-value-report.md — sponsor Markdown when available.
                     - first-value-report.pdf — when PDF build succeeded.
                     - sponsor-one-pager.pdf — when Standard-tier scorecard path succeeded (may be absent on Team tier).

                     Legal: obtain a signed reference agreement before publishing externally.
                     """;

                ZipArchiveEntry readmeEntry = zip.CreateEntry("README.txt");
                await using (Stream s = readmeEntry.Open())
                {
                    byte[] r = Encoding.UTF8.GetBytes(readme);
                    await s.WriteAsync(r, cancellationToken);
                }
            }
        }

        return zipStream.ToArray();
    }
}
