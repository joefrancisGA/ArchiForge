using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Contracts;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Packaging;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Queries;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for listing, downloading, and packaging synthesized artifacts produced for a golden manifest.
/// </summary>
/// <remarks>
/// Routes are prefixed <c>api/artifacts</c> and require the <see cref="ArchiForgePolicies.ReadAuthority"/> policy.
/// Artifact descriptors are resolved from the artifact query service; packaging (ZIP export) is performed
/// by <see cref="IArtifactPackagingService"/>. All download operations emit an <c>ArtifactExported</c> audit event.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/artifacts")]
[EnableRateLimiting("fixed")]
public sealed class ArtifactExportController(
    IArtifactQueryService artifactQueryService,
    IAuthorityQueryService authorityQueryService,
    IArtifactPackagingService artifactPackagingService,
    IScopeContextProvider scopeProvider,
    IAuditService auditService)
    : ControllerBase
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet("manifests/{manifestId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ArtifactDescriptorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ArtifactDescriptorResponse>>> ListArtifacts(
        Guid manifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        IReadOnlyList<ArtifactDescriptor> artifacts = await artifactQueryService.ListArtifactsByManifestIdAsync(scope, manifestId, ct);

        return Ok(artifacts.Select(x => new ArtifactDescriptorResponse
        {
            ArtifactId = x.ArtifactId,
            ArtifactType = x.ArtifactType,
            Name = x.Name,
            Format = x.Format,
            CreatedUtc = x.CreatedUtc,
            ContentHash = x.ContentHash
        }).ToList());
    }

    [HttpGet("manifests/{manifestId:guid}/artifact/{artifactId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadArtifact(
        Guid manifestId,
        Guid artifactId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        SynthesizedArtifact? artifact = await artifactQueryService.GetArtifactByIdAsync(scope, manifestId, artifactId, ct);
        if (artifact is null)
            return this.NotFoundProblem($"Artifact '{artifactId}' was not found for manifest '{manifestId}'.", ProblemTypes.ResourceNotFound);

        ArtifactFileExport file = artifactPackagingService.BuildSingleFileExport(artifact);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ArtifactDownloaded,
                ManifestId = manifestId,
                ArtifactId = artifactId
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("manifests/{manifestId:guid}/bundle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadBundle(
        Guid manifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        IReadOnlyList<SynthesizedArtifact> artifacts = await artifactQueryService.GetArtifactsByManifestIdAsync(scope, manifestId, ct);
        if (artifacts.Count == 0)
            return this.NotFoundProblem($"No artifacts were found for manifest '{manifestId}'.", ProblemTypes.ManifestNotFound);

        ArtifactPackage package = artifactPackagingService.BuildBundlePackage(manifestId, artifacts);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.BundleDownloaded,
                ManifestId = manifestId
            },
            ct);

        return File(package.Content, package.ContentType, package.PackageFileName);
    }

    [HttpGet("runs/{runId:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadRunExport(
        Guid runId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? runDetail = await authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (runDetail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        if (runDetail.GoldenManifest is null)
            return this.NotFoundProblem($"Run '{runId}' has no committed golden manifest available for export.", ProblemTypes.ManifestNotFound);

        IReadOnlyList<SynthesizedArtifact> artifacts = await artifactQueryService.GetArtifactsByManifestIdAsync(
            scope,
            runDetail.GoldenManifest.ManifestId,
            ct);

        string manifestJson = JsonSerializer.Serialize(runDetail.GoldenManifest, ExportJsonOptions);

        string? traceJson = runDetail.DecisionTrace is null
            ? null
            : JsonSerializer.Serialize(runDetail.DecisionTrace, ExportJsonOptions);

        ArtifactPackage package = artifactPackagingService.BuildRunExportPackage(
            runId,
            runDetail.GoldenManifest.ManifestId,
            artifacts,
            manifestJson,
            traceJson);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RunExported,
                RunId = runId,
                ManifestId = runDetail.GoldenManifest.ManifestId
            },
            ct);

        return File(package.Content, package.ContentType, package.PackageFileName);
    }
}
