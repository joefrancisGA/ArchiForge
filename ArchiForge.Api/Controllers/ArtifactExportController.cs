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
[Route("v{version:apiVersion}/artifacts")]
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

    /// <summary>
    /// Lists artifact descriptors for a golden manifest. Returns <c>200 OK</c> with a JSON array (possibly empty)
    /// sorted by name then id; <c>404</c> when the manifest is missing in the current scope.
    /// </summary>
    [HttpGet("manifests/{manifestId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ArtifactDescriptorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListArtifacts(
        Guid manifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (await authorityQueryService.GetManifestSummaryAsync(scope, manifestId, ct) is null)
        
            return this.NotFoundProblem(
                $"Manifest '{manifestId}' was not found in the current scope.",
                ProblemTypes.ManifestNotFound);
        

        IReadOnlyList<ArtifactDescriptor> artifacts = await artifactQueryService.ListArtifactsByManifestIdAsync(scope, manifestId, ct);

        return Ok(artifacts.Select(a => ArtifactDescriptorResponse.From(a, manifestId)).ToList());
    }

    /// <summary>
    /// JSON metadata for one artifact (operator review). <c>404</c> if the manifest is out of scope or the artifact id is not in that manifest’s bundle.
    /// </summary>
    [HttpGet("manifests/{manifestId:guid}/artifact/{artifactId:guid}/descriptor")]
    [ProducesResponseType(typeof(ArtifactDescriptorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetArtifactDescriptor(
        Guid manifestId,
        Guid artifactId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (await authorityQueryService.GetManifestSummaryAsync(scope, manifestId, ct) is null)
        
            return this.NotFoundProblem(
                $"Manifest '{manifestId}' was not found in the current scope.",
                ProblemTypes.ManifestNotFound);
        

        SynthesizedArtifact? artifact = await artifactQueryService.GetArtifactByIdAsync(scope, manifestId, artifactId, ct);
        if (artifact is null)
        
            return this.NotFoundProblem(
                $"Artifact '{artifactId}' was not found for manifest '{manifestId}'.",
                ProblemTypes.ResourceNotFound);
        

        return Ok(ArtifactDescriptorResponse.From(artifact));
    }

    /// <summary>Downloads one synthesized artifact file. Requires manifest in scope; same 404 semantics as the descriptor endpoint.</summary>
    [HttpGet("manifests/{manifestId:guid}/artifact/{artifactId:guid}")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadArtifact(
        Guid manifestId,
        Guid artifactId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (await authorityQueryService.GetManifestSummaryAsync(scope, manifestId, ct) is null)
        
            return this.NotFoundProblem(
                $"Manifest '{manifestId}' was not found in the current scope.",
                ProblemTypes.ManifestNotFound);
        

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

    /// <summary>
    /// ZIP of all artifacts for the manifest (stable entry order: name then id). <c>404</c> with manifest-not-found when
    /// the manifest is missing; <c>404</c> with resource-not-found when the manifest exists but has no bundle or zero artifacts.
    /// </summary>
    [HttpGet("manifests/{manifestId:guid}/bundle")]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadBundle(
        Guid manifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (await authorityQueryService.GetManifestSummaryAsync(scope, manifestId, ct) is null)
        
            return this.NotFoundProblem(
                $"Manifest '{manifestId}' was not found in the current scope.",
                ProblemTypes.ManifestNotFound);
        

        IReadOnlyList<SynthesizedArtifact> artifacts = await artifactQueryService.GetArtifactsByManifestIdAsync(scope, manifestId, ct);
        if (artifacts.Count == 0)
        
            return this.NotFoundProblem(
                $"Manifest '{manifestId}' has no artifact bundle or the bundle contains no artifacts. " +
                $"The list endpoint GET api/artifacts/manifests/{manifestId} returns an empty JSON array when there are no artifact rows.",
                ProblemTypes.ResourceNotFound);
        

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

    /// <summary>ZIP export of run manifest, trace, and artifacts when the run is committed; artifacts ordered like the manifest bundle list.</summary>
    [HttpGet("runs/{runId:guid}/export")]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        string? traceJson = runDetail.AuthorityTrace is null
            ? null
            : JsonSerializer.Serialize(runDetail.AuthorityTrace, ExportJsonOptions);

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
