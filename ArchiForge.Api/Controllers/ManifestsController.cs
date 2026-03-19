using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Summaries;
using ArchiForge.Data.Repositories;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class ManifestsController(
    IArchitectureApplicationService architectureApplicationService,
    IGoldenManifestRepository manifestRepository,
    IManifestDiffService manifestDiffService,
    IManifestDiffSummaryFormatter manifestDiffSummaryFormatter,
    IManifestDiffExportService manifestDiffExportService,
    IDiagramGenerator diagramGenerator,
    IManifestSummaryGenerator summaryGenerator,
    IManifestSummaryService manifestSummaryService,
    IArchitectureExportService exportService,
    IAgentEvidencePackageRepository agentEvidencePackageRepository)
    : ControllerBase
{
    [HttpGet("manifest/compare")]
    [ProducesResponseType(typeof(ManifestCompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifests(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var right = await manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var diff = manifestDiffService.Compare(left, right);

        return Ok(new ManifestCompareResponse
        {
            LeftManifest = left,
            RightManifest = right,
            Diff = diff
        });
    }

    [HttpGet("manifest/compare/summary")]
    [ProducesResponseType(typeof(ManifestCompareSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifestsSummary(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var right = await manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var diff = manifestDiffService.Compare(left, right);
        var summary = manifestDiffSummaryFormatter.FormatMarkdown(diff);

        return Ok(new ManifestCompareSummaryResponse
        {
            LeftManifestVersion = leftVersion,
            RightManifestVersion = rightVersion,
            Format = "markdown",
            Summary = summary,
            Diff = diff
        });
    }

    [HttpGet("manifest/compare/export")]
    [ProducesResponseType(typeof(ManifestCompareExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifestsExport(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var right = await manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var diff = manifestDiffService.Compare(left, right);
        var summary = manifestDiffSummaryFormatter.FormatMarkdown(diff);
        var content = manifestDiffExportService.GenerateMarkdownExport(left, right, diff, summary);

        return Ok(new ManifestCompareExportResponse
        {
            LeftManifestVersion = leftVersion,
            RightManifestVersion = rightVersion,
            Format = "markdown",
            FileName = $"compare_{leftVersion}_to_{rightVersion}.md",
            Content = content
        });
    }

    [HttpGet("manifest/compare/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCompareManifestsExport(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var right = await manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);

        var diff = manifestDiffService.Compare(left, right);
        var summary = manifestDiffSummaryFormatter.FormatMarkdown(diff);
        var content = manifestDiffExportService.GenerateMarkdownExport(left, right, diff, summary);

        var fileName = $"compare_{leftVersion}_to_{rightVersion}.md";
        return ApiFileResults.RangeText(Request, content, "text/markdown", fileName);
    }

    [HttpGet("manifest/{version}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await architectureApplicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        return Ok(manifest);
    }

    [HttpGet("manifest/{version}/diagram")]
    [ProducesResponseType(typeof(DiagramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestDiagram(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await architectureApplicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        var mermaid = diagramGenerator.GenerateMermaid(manifest);

        return Ok(new DiagramResponse
        {
            ManifestVersion = version,
            Format = "mermaid",
            Diagram = mermaid
        });
    }

    [HttpGet("manifest/{version}/diagram/v2")]
    [ProducesResponseType(typeof(ManifestDiagramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestDiagramV2(
        [FromRoute] string version,
        [FromQuery] string? layout = "LR",
        [FromQuery] bool includeRuntimePlatform = true,
        [FromQuery] string? relationshipLabels = "type",
        [FromQuery] string? groupBy = "none",
        [FromServices] IManifestDiagramService manifestDiagramService = null!,
        CancellationToken cancellationToken = default)
    {
        var manifest = await architectureApplicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        var opts = new ManifestDiagramOptions
        {
            Layout = layout ?? "LR",
            IncludeRuntimePlatform = includeRuntimePlatform,
            RelationshipLabels = relationshipLabels ?? "type",
            GroupBy = groupBy ?? "none"
        };

        var mermaid = manifestDiagramService.GenerateMermaid(manifest, opts);

        return Ok(new ManifestDiagramResponse
        {
            ManifestVersion = version,
            DiagramType = "Mermaid",
            Content = mermaid
        });
    }

    [HttpGet("manifest/{version}/summary")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestSummary(
        [FromRoute] string version,
        [FromQuery] string? format = "markdown",
        [FromQuery] bool includeRelationships = true,
        [FromQuery] bool includeRequiredControls = true,
        [FromQuery] bool includeTags = true,
        [FromQuery] bool includeComponentControls = true,
        [FromQuery] int? maxRelationships = null,
        CancellationToken cancellationToken = default)
    {
        var manifest = await manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new ManifestSummaryJsonResponse
            {
                ManifestVersion = version,
                SystemName = manifest.SystemName,
                ServiceCount = manifest.Services.Count,
                DatastoreCount = manifest.Datastores.Count,
                RelationshipCount = manifest.Relationships.Count,
                RequiredControls = includeRequiredControls
                    ? manifest.Governance.RequiredControls.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                    : [],
                Services = manifest.Services
                    .OrderBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new ManifestSummaryServiceItem
                    {
                        Name = s.ServiceName,
                        ServiceType = s.ServiceType.ToString(),
                        RuntimePlatform = s.RuntimePlatform.ToString(),
                        Purpose = s.Purpose,
                        RequiredControls = includeComponentControls
                            ? s.RequiredControls.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                            : [],
                        Tags = includeTags
                            ? s.Tags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                            : []
                    })
                    .ToList(),
                Datastores = manifest.Datastores
                    .OrderBy(d => d.DatastoreName, StringComparer.OrdinalIgnoreCase)
                    .Select(d => new ManifestSummaryDatastoreItem
                    {
                        Name = d.DatastoreName,
                        DatastoreType = d.DatastoreType.ToString(),
                        RuntimePlatform = d.RuntimePlatform.ToString(),
                        Purpose = d.Purpose,
                        PrivateEndpointRequired = d.PrivateEndpointRequired,
                        EncryptionAtRestRequired = d.EncryptionAtRestRequired
                    })
                    .ToList(),
                Relationships = includeRelationships
                    ? manifest.Relationships.Take(maxRelationships ?? int.MaxValue).Select(r => new ManifestSummaryRelationshipItem
                    {
                        SourceId = r.SourceId,
                        TargetId = r.TargetId,
                        RelationshipType = r.RelationshipType.ToString(),
                        Description = r.Description
                    }).ToList()
                    : []
            });
        }

        if (!string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            return this.BadRequestProblem(
                "format must be 'markdown' or 'json'.",
                ProblemTypes.ValidationFailed);
        }

        var options = new ManifestSummaryOptions
        {
            IncludeRelationships = includeRelationships,
            IncludeRequiredControls = includeRequiredControls,
            IncludeTags = includeTags,
            IncludeComponentControls = includeComponentControls,
            MaxRelationships = maxRelationships
        };

        var content = manifestSummaryService.GenerateMarkdown(manifest, options);

        return Ok(new ManifestSummaryResponse
        {
            ManifestVersion = version,
            Format = "markdown",
            Content = content,
            Summary = content
        });
    }

    [HttpGet("manifest/{version}/summary/evidence")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestSummaryEvidence(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var markdown = summaryGenerator.GenerateMarkdown(manifest, evidence);

        return Ok(new ManifestSummaryResponse
        {
            ManifestVersion = version,
            Format = "markdown",
            Content = markdown,
            Summary = markdown
        });
    }

    [HttpGet("manifest/{version}/bundle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestBundle(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var diagram = diagramGenerator.GenerateMermaid(manifest);
        var summary = summaryGenerator.GenerateMarkdown(manifest, evidence);

        return Ok(new
        {
            manifestVersion = version,
            manifest,
            diagram,
            summary
        });
    }

    [HttpGet("manifest/{version}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestExport(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var diagram = diagramGenerator.GenerateMermaid(manifest);
        var summary = summaryGenerator.GenerateMarkdown(manifest, evidence);
        var markdown = exportService.GenerateMarkdownPackage(manifest, diagram, summary, evidence);

        return Ok(new { manifestVersion = version, format = "markdown", content = markdown });
    }

    [HttpGet("manifest/{version}/export/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadManifestExport(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var diagram = diagramGenerator.GenerateMermaid(manifest);
        var summary = summaryGenerator.GenerateMarkdown(manifest, evidence);
        var markdown = exportService.GenerateMarkdownPackage(manifest, diagram, summary, evidence);

        var fileName = $"architecture-export-{version}.md";
        return ApiFileResults.RangeText(Request, markdown, "text/markdown", fileName);
    }
}

