using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;
/// <inheritdoc cref = "IReferenceEvidenceAdminExportService"/>
public sealed class ReferenceEvidenceAdminExportService(IReferenceEvidenceRunLookup runLookup, IRunDetailQueryService runDetailQuery, IPilotRunDeltaComputer deltaComputer, FirstValueReportBuilder firstValueReportBuilder, FirstValueReportPdfBuilder firstValueReportPdfBuilder, SponsorOnePagerPdfBuilder sponsorOnePagerPdfBuilder, ILogger<ReferenceEvidenceAdminExportService> logger) : IReferenceEvidenceAdminExportService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runLookup, runDetailQuery, deltaComputer, firstValueReportBuilder, firstValueReportPdfBuilder, sponsorOnePagerPdfBuilder, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Interfaces.IReferenceEvidenceRunLookup runLookup, ArchLucid.Application.IRunDetailQueryService runDetailQuery, ArchLucid.Application.Pilots.IPilotRunDeltaComputer deltaComputer, ArchLucid.Application.Pilots.FirstValueReportBuilder firstValueReportBuilder, ArchLucid.Application.Pilots.FirstValueReportPdfBuilder firstValueReportPdfBuilder, ArchLucid.Application.Pilots.SponsorOnePagerPdfBuilder sponsorOnePagerPdfBuilder, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.Pilots.ReferenceEvidenceAdminExportService> logger)
    {
        ArgumentNullException.ThrowIfNull(runLookup);
        ArgumentNullException.ThrowIfNull(runDetailQuery);
        ArgumentNullException.ThrowIfNull(deltaComputer);
        ArgumentNullException.ThrowIfNull(firstValueReportBuilder);
        ArgumentNullException.ThrowIfNull(firstValueReportPdfBuilder);
        ArgumentNullException.ThrowIfNull(sponsorOnePagerPdfBuilder);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly IPilotRunDeltaComputer _deltaComputer = deltaComputer ?? throw new ArgumentNullException(nameof(deltaComputer));
    private readonly FirstValueReportBuilder _firstValueReportBuilder = firstValueReportBuilder ?? throw new ArgumentNullException(nameof(firstValueReportBuilder));
    private readonly FirstValueReportPdfBuilder _firstValueReportPdfBuilder = firstValueReportPdfBuilder ?? throw new ArgumentNullException(nameof(firstValueReportPdfBuilder));
    private readonly ILogger<ReferenceEvidenceAdminExportService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRunDetailQueryService _runDetailQuery = runDetailQuery ?? throw new ArgumentNullException(nameof(runDetailQuery));
    private readonly IReferenceEvidenceRunLookup _runLookup = runLookup ?? throw new ArgumentNullException(nameof(runLookup));
    private readonly SponsorOnePagerPdfBuilder _sponsorOnePagerPdfBuilder = sponsorOnePagerPdfBuilder ?? throw new ArgumentNullException(nameof(sponsorOnePagerPdfBuilder));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<System.Byte[]?> BuildZipAsync(Guid tenantId, bool includeDemo, string apiBaseForLinks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(apiBaseForLinks);
        IReadOnlyList<ReferenceEvidenceRunCandidate> candidates = await _runLookup.ListRecentCommittedRunsAsync(tenantId, 200, cancellationToken);
        ReferenceEvidenceRunCandidate? selected = (
            from row in candidates
            let runKey = row.RunId.ToString("N")
            where includeDemo || (!ContosoRetailDemoIdentifiers.IsDemoRunId(runKey) && !ContosoRetailDemoIdentifiers.IsDemoRequestId(row.RequestId))select row).FirstOrDefault();
        if (selected is null)
            return null;
        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = selected.WorkspaceId,
            ProjectId = selected.ScopeProjectId
        };
        string runId = selected.RunId.ToString("N");
        string baseUrl = string.IsNullOrWhiteSpace(apiBaseForLinks) ? "http://localhost:5000" : apiBaseForLinks.Trim().TrimEnd('/');
        using MemoryStream zipStream = new();
        await using (ZipArchive zip = new(zipStream, ZipArchiveMode.Create, true))
        {
            using (IDisposable _ = AmbientScopeContext.Push(scope))
            {
                if (await _runDetailQuery.GetRunDetailAsync(runId, cancellationToken)is not { } detail)
                    return null;
                PilotRunDeltas deltas = await _deltaComputer.ComputeAsync(detail, cancellationToken);
                PilotRunDeltasResponse deltaDto = PilotRunDeltasResponseMapper.ToResponse(deltas);
                byte[] deltasJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(deltaDto, JsonOptions));
                ZipArchiveEntry deltasEntry = zip.CreateEntry("pilot-run-deltas.json");
                await using (Stream s = await deltasEntry.OpenAsync(cancellationToken))
                {
                    await s.WriteAsync(deltasJson, cancellationToken);
                }

                string? markdown = await _firstValueReportBuilder.BuildMarkdownAsync(runId, baseUrl, cancellationToken);
                if (markdown is not null)
                {
                    ZipArchiveEntry mdEntry = zip.CreateEntry("first-value-report.md");
                    await using Stream s = await mdEntry.OpenAsync(cancellationToken);
                    byte[] mdBytes = Encoding.UTF8.GetBytes(markdown);
                    await s.WriteAsync(mdBytes, cancellationToken);
                }

                try
                {
                    byte[]? firstPdf = await _firstValueReportPdfBuilder.BuildPdfAsync(runId, baseUrl, cancellationToken);
                    if (firstPdf is { Length: > 0 })
                    {
                        ZipArchiveEntry pdfEntry = zip.CreateEntry("first-value-report.pdf");
                        await using Stream s = await pdfEntry.OpenAsync(cancellationToken);
                        await s.WriteAsync(firstPdf, cancellationToken);
                    }
                }
                catch (Exception ex)when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reference evidence: first-value PDF omitted for run {RunId}.", runId);
                }

                try
                {
                    byte[]? sponsorPdf = await _sponsorOnePagerPdfBuilder.BuildPdfAsync(runId, baseUrl, cancellationToken);
                    if (sponsorPdf is { Length: > 0 })
                    {
                        ZipArchiveEntry spEntry = zip.CreateEntry("sponsor-one-pager.pdf");
                        await using Stream s = await spEntry.OpenAsync(cancellationToken);
                        await s.WriteAsync(sponsorPdf, cancellationToken);
                    }
                }
                catch (Exception ex)when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reference evidence: sponsor one-pager omitted for run {RunId}.", runId);
                }

                string readme = $"""
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
                     - proof-pack-readme.md — buyer-oriented Markdown overview (redaction + file table).

                     Legal: obtain a signed reference agreement before publishing externally.
                     """;
                ZipArchiveEntry readmeEntry = zip.CreateEntry("README.txt");
                await using (Stream s = await readmeEntry.OpenAsync(cancellationToken))
                {
                    byte[] r = Encoding.UTF8.GetBytes(readme);
                    await s.WriteAsync(r, cancellationToken);
                }

                string proofPackReadme = BuildProofPackReadmeMarkdown(tenantId, runId, includeDemo);
                ZipArchiveEntry proofReadmeEntry = zip.CreateEntry("proof-pack-readme.md");
                await using (Stream s = await proofReadmeEntry.OpenAsync(cancellationToken))
                {
                    byte[] md = Encoding.UTF8.GetBytes(proofPackReadme);
                    await s.WriteAsync(md, cancellationToken);
                }
            }
        }

        return zipStream.ToArray();
    }

    private static string BuildProofPackReadmeMarkdown(Guid tenantId, string runId, bool includeDemo)
    {
        return $"""
                # ArchLucid proof pack (reference evidence)

                This ZIP packages **committed-run** artifacts for diligence: deltas JSON, first-value narrative (Markdown/PDF when built), and sponsor one-pager (PDF when available).

                ## Redaction and external use

                - Treat as **confidential** until your legal team approves external sharing.
                - When redacting for buyers or anonymous benchmarks, follow **`docs/library/PROOF_PACK_REDACTION_PROFILES.md`** in the ArchLucid repository.

                ## Files

                | File | Description |
                | --- | --- |
                | `pilot-run-deltas.json` | Proof-of-ROI / delta numbers backing the narrative. |
                | `first-value-report.md` | Sponsor Markdown when the builder produced it. |
                | `first-value-report.pdf` | Rendered first-value report when PDF generation succeeded. |
                | `sponsor-one-pager.pdf` | Standard-tier scorecard path when available. |
                | `README.txt` | Short bundle metadata. |
                | `proof-pack-readme.md` | This file (Markdown overview for humans). |

                ## Bundle metadata

                - **TenantId:** `{tenantId:D}`
                - **RunId:** `{runId}`
                - **IncludeDemo:** {includeDemo}
                - **GeneratedUtc:** {DateTime.UtcNow:O}

                Obtain a **signed reference agreement** before publishing customer-specific metrics externally.
                """;
    }
}