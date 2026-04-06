using ArchiForge.AgentRuntime.Explanation;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.ArtifactSynthesis.Docx;
using ArchiForge.ArtifactSynthesis.Docx.Models;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Provenance;
using ArchiForge.Persistence.Queries;
using ArchiForge.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Downloads a Word architecture package for a run, with optional compare run, optional run explanation, and optional comparison narrative.
/// </summary>
/// <remarks>Route prefix <c>api/docx</c>; combines <see cref="IAuthorityQueryService"/>, artifacts, <see cref="IComparisonService"/>, and <see cref="IExplanationService"/>.</remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/docx")]
[EnableRateLimiting("fixed")]
public sealed class DocxExportController(
    IAuthorityQueryService authorityQueryService,
    IArtifactQueryService artifactQueryService,
    IDocxExportService docxExportService,
    IComparisonService comparisonService,
    IExplanationService explanationService,
    IProvenanceSnapshotRepository provenanceSnapshotRepository,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Streams a DOCX architecture package for <paramref name="runId"/>.</summary>
    /// <param name="runId">Primary run (must have golden manifest).</param>
    /// <param name="compareWithRunId">When set, embeds manifest comparison (and optional comparison narrative) vs this run.</param>
    /// <param name="explainRun">When <see langword="true"/>, generates run-level <see cref="ExplanationResult"/> via LLM.</param>
    /// <param name="includeComparisonExplanation">When <see langword="true"/> and comparison exists, generates <see cref="ComparisonExplanationResult"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>DOCX file download, or 404 when primary (or compare) run/manifest is missing.</returns>
    [HttpGet("runs/{runId:guid}/architecture-package")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunDocx(
        Guid runId,
        [FromQuery] Guid? compareWithRunId,
        [FromQuery] bool explainRun = false,
        [FromQuery] bool includeComparisonExplanation = true,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? runDetail = await authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (runDetail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        if (runDetail.GoldenManifest is null)
            return this.NotFoundProblem($"Run '{runId}' does not have a committed golden manifest.", ProblemTypes.ManifestNotFound);

        GoldenManifest? manifest = runDetail.GoldenManifest;
        IReadOnlyList<SynthesizedArtifact> artifacts = await artifactQueryService.GetArtifactsByManifestIdAsync(
            scope,
            manifest.ManifestId,
            ct);

        ComparisonResult? manifestComparison = null;
        if (compareWithRunId is not null)
        {
            RunDetailDto? targetDetail = await authorityQueryService.GetRunDetailAsync(scope, compareWithRunId.Value, ct);
            if (targetDetail is null)
                return this.NotFoundProblem($"Compare run '{compareWithRunId.Value}' was not found.", ProblemTypes.RunNotFound);
            if (targetDetail.GoldenManifest is null)
                return this.NotFoundProblem($"Compare run '{compareWithRunId.Value}' does not have a committed golden manifest.", ProblemTypes.ManifestNotFound);
            manifestComparison = comparisonService.Compare(manifest, targetDetail.GoldenManifest);
        }

        ComparisonExplanationResult? comparisonNarrative = null;
        if (manifestComparison is not null && includeComparisonExplanation)
            comparisonNarrative = await explanationService.ExplainComparisonAsync(manifestComparison, ct);

        ExplanationResult? runNarrative = null;
        if (explainRun)
        {
            DecisionProvenanceSnapshot? snapshot = await provenanceSnapshotRepository.GetByRunIdAsync(scope, runId, ct);
            DecisionProvenanceGraph? graph = snapshot is null ? null : ProvenanceGraphSerializer.Deserialize(snapshot.GraphJson);
            runNarrative = await explanationService.ExplainRunAsync(manifest, graph, ct);
        }

        DocxExportResult result = await docxExportService.ExportAsync(
            DocxExportRequest.ForArchitecturePackage(
                runId,
                manifest.ManifestId,
                "ArchiForge Architecture Package",
                $"Generated for Run {runId}",
                manifestComparison,
                comparisonNarrative,
                runNarrative,
                runDetail.FindingsSnapshot),
            manifest,
            artifacts,
            ct);

        return File(result.Content, result.ContentType, result.FileName);
    }
}
