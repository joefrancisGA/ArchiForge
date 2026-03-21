using ArchiForge.Api.Auth.Models;
using ArchiForge.ArtifactSynthesis.Docx;
using ArchiForge.ArtifactSynthesis.Docx.Models;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Persistence.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/docx")]
[EnableRateLimiting("fixed")]
public sealed class DocxExportController : ControllerBase
{
    private readonly IAuthorityQueryService _authorityQueryService;
    private readonly IArtifactQueryService _artifactQueryService;
    private readonly IDocxExportService _docxExportService;
    private readonly IComparisonService _comparisonService;
    private readonly IScopeContextProvider _scopeProvider;

    public DocxExportController(
        IAuthorityQueryService authorityQueryService,
        IArtifactQueryService artifactQueryService,
        IDocxExportService docxExportService,
        IComparisonService comparisonService,
        IScopeContextProvider scopeProvider)
    {
        _authorityQueryService = authorityQueryService;
        _artifactQueryService = artifactQueryService;
        _docxExportService = docxExportService;
        _comparisonService = comparisonService;
        _scopeProvider = scopeProvider;
    }

    [HttpGet("runs/{runId:guid}/architecture-package")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunDocx(
        Guid runId,
        [FromQuery] Guid? compareWithRunId,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var runDetail = await _authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (runDetail?.GoldenManifest is null)
            return NotFound();

        var manifest = runDetail.GoldenManifest;
        var artifacts = await _artifactQueryService.GetArtifactsByManifestIdAsync(
            scope,
            manifest.ManifestId,
            ct);

        ComparisonResult? manifestComparison = null;
        if (compareWithRunId is not null)
        {
            var targetDetail = await _authorityQueryService.GetRunDetailAsync(scope, compareWithRunId.Value, ct);
            if (targetDetail?.GoldenManifest is null)
                return NotFound();
            manifestComparison = _comparisonService.Compare(manifest, targetDetail.GoldenManifest);
        }

        var result = await _docxExportService.ExportAsync(
            new DocxExportRequest
            {
                RunId = runId,
                ManifestId = manifest.ManifestId,
                DocumentTitle = "ArchiForge Architecture Package",
                Subtitle = $"Generated for Run {runId}",
                ManifestComparison = manifestComparison
            },
            manifest,
            artifacts,
            ct);

        return File(result.Content, result.ContentType, result.FileName);
    }
}
