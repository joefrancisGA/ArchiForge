using ArchLucid.Core.Authorization;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Diagrams;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Exports;
using ArchLucid.Application.Summaries;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Host.Core.Services;
using ArchLucid.Persistence.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Governance;

/// <summary>
/// Provides read access to golden manifests, manifest diffs, and manifest-level export operations.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class ManifestsController(
    IArchitectureApplicationService architectureApplicationService,
    ICoordinatorGoldenManifestRepository manifestRepository,
    IManifestDiffService manifestDiffService,
    IManifestDiffSummaryFormatter manifestDiffSummaryFormatter,
    IManifestDiffExportService manifestDiffExportService,
    IDiagramGenerator diagramGenerator,
    IManifestSummaryGenerator summaryGenerator,
    IManifestSummaryService manifestSummaryService,
    IArchitectureExportService exportService,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IManifestDiagramService manifestDiagramService)
    : ControllerBase
{
    private const string FormatMarkdown = "markdown";
    private const string FormatJson = "json";
    private const string FormatMermaid = "mermaid";
    private const string DiagramTypeMermaid = "Mermaid";
    private const string DiagramLayoutDefault = "LR";
    private const string RelationshipLabelsDefault = "type";
    private const string GroupByDefault = "none";
    [HttpGet("manifest/compare")]
    [ProducesResponseType(typeof(ManifestCompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifests(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        LoadedManifestPair loaded = await LoadAndCompareManifestPairAsync(leftVersion, rightVersion, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        return Ok(new ManifestCompareResponse
        {
            LeftManifest = loaded.Left!,
            RightManifest = loaded.Right!,
            Diff = loaded.Diff!
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
        LoadedManifestPair loaded = await LoadAndCompareManifestPairAsync(leftVersion, rightVersion, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        string summary = manifestDiffSummaryFormatter.FormatMarkdown(loaded.Diff!);

        return Ok(new ManifestCompareSummaryResponse
        {
            LeftManifestVersion = leftVersion,
            RightManifestVersion = rightVersion,
            Format = FormatMarkdown,
            Summary = summary,
            Diff = loaded.Diff!
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
        LoadedManifestPair loaded = await LoadAndCompareManifestPairAsync(leftVersion, rightVersion, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        string summary = manifestDiffSummaryFormatter.FormatMarkdown(loaded.Diff!);
        string content = manifestDiffExportService.GenerateMarkdownExport(loaded.Left!, loaded.Right!, loaded.Diff!, summary);

        return Ok(new ManifestCompareExportResponse
        {
            LeftManifestVersion = leftVersion,
            RightManifestVersion = rightVersion,
            Format = FormatMarkdown,
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
        LoadedManifestPair loaded = await LoadAndCompareManifestPairAsync(leftVersion, rightVersion, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        string summary = manifestDiffSummaryFormatter.FormatMarkdown(loaded.Diff!);
        string content = manifestDiffExportService.GenerateMarkdownExport(loaded.Left!, loaded.Right!, loaded.Diff!, summary);

        string fileName = $"compare_{leftVersion}_to_{rightVersion}.md";
        return ApiFileResults.RangeText(Request, content, "text/markdown", fileName);
    }

    [HttpGet("manifest/{manifestVersion}")]
    [ProducesResponseType(typeof(GoldenManifest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(
        [FromRoute] string manifestVersion,
        CancellationToken cancellationToken)
    {
        GoldenManifest? manifest = await architectureApplicationService.GetManifestAsync(manifestVersion, cancellationToken);
        return manifest is null ? this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound) : Ok(manifest);
    }

    [HttpGet("manifest/{manifestVersion}/diagram")]
    [ProducesResponseType(typeof(DiagramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestDiagram(
        [FromRoute] string manifestVersion,
        CancellationToken cancellationToken)
    {
        GoldenManifest? manifest = await architectureApplicationService.GetManifestAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        string mermaid = diagramGenerator.GenerateMermaid(manifest);

        return Ok(new DiagramResponse
        {
            ManifestVersion = manifestVersion,
            Format = FormatMermaid,
            Diagram = mermaid
        });
    }

    [HttpGet("manifest/{manifestVersion}/diagram/v2")]
    [ProducesResponseType(typeof(ManifestDiagramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestDiagramV2(
        [FromRoute] string manifestVersion,
        [FromQuery] string? layout = DiagramLayoutDefault,
        [FromQuery] bool includeRuntimePlatform = true,
        [FromQuery] string? relationshipLabels = RelationshipLabelsDefault,
        [FromQuery] string? groupBy = GroupByDefault,
        CancellationToken cancellationToken = default)
    {
        GoldenManifest? manifest = await architectureApplicationService.GetManifestAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        ManifestDiagramOptions opts = new()
        {
            Layout = layout ?? DiagramLayoutDefault,
            IncludeRuntimePlatform = includeRuntimePlatform,
            RelationshipLabels = relationshipLabels ?? RelationshipLabelsDefault,
            GroupBy = groupBy ?? GroupByDefault
        };

        string mermaid = manifestDiagramService.GenerateMermaid(manifest, opts);

        return Ok(new ManifestDiagramResponse
        {
            ManifestVersion = manifestVersion,
            DiagramType = DiagramTypeMermaid,
            Content = mermaid
        });
    }

    [HttpGet("manifest/{manifestVersion}/summary")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestSummary(
        [FromRoute] string manifestVersion,
        [FromQuery] string? format = "markdown",
        [FromQuery] bool includeRelationships = true,
        [FromQuery] bool includeRequiredControls = true,
        [FromQuery] bool includeTags = true,
        [FromQuery] bool includeComponentControls = true,
        [FromQuery] int? maxRelationships = null,
        CancellationToken cancellationToken = default)
    {
        GoldenManifest? manifest = await manifestRepository.GetByVersionAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        int? clampedMaxRelationships = maxRelationships.HasValue
            ? Math.Clamp(maxRelationships.Value, 1, ManifestSummaryLimits.MaxRelationships)
            : null;

        if (string.Equals(format, FormatJson, StringComparison.OrdinalIgnoreCase))

            return Ok(new ManifestSummaryJsonResponse
            {
                ManifestVersion = manifestVersion,
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
                    ? manifest.Relationships.Take(clampedMaxRelationships ?? int.MaxValue).Select(r => new ManifestSummaryRelationshipItem
                    {
                        SourceId = r.SourceId,
                        TargetId = r.TargetId,
                        RelationshipType = r.RelationshipType.ToString(),
                        Description = r.Description
                    }).ToList()
                    : []
            });


        if (!string.Equals(format, FormatMarkdown, StringComparison.OrdinalIgnoreCase))

            return this.BadRequestProblem(
                $"format must be '{FormatMarkdown}' or '{FormatJson}'.",
                ProblemTypes.ValidationFailed);


        ManifestSummaryOptions options = new()
        {
            IncludeRelationships = includeRelationships,
            IncludeRequiredControls = includeRequiredControls,
            IncludeTags = includeTags,
            IncludeComponentControls = includeComponentControls,
            MaxRelationships = clampedMaxRelationships
        };

        string content = manifestSummaryService.GenerateMarkdown(manifest, options);

        return Ok(new ManifestSummaryResponse
        {
            ManifestVersion = manifestVersion,
            Format = FormatMarkdown,
            Content = content,
            Summary = content
        });
    }

    [HttpGet("manifest/{manifestVersion}/summary/evidence")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestSummaryEvidence(
        [FromRoute] string manifestVersion,
        CancellationToken cancellationToken)
    {
        (GoldenManifest? manifest, AgentEvidencePackage? evidence) = await LoadManifestWithEvidenceAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        string markdown = summaryGenerator.GenerateMarkdown(manifest, evidence);

        return Ok(new ManifestSummaryResponse
        {
            ManifestVersion = manifestVersion,
            Format = FormatMarkdown,
            Content = markdown,
            Summary = markdown
        });
    }

    [HttpGet("manifest/{manifestVersion}/bundle")]
    [ProducesResponseType(typeof(ManifestBundleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestBundle(
        [FromRoute] string manifestVersion,
        CancellationToken cancellationToken)
    {
        (GoldenManifest? manifest, AgentEvidencePackage? evidence) = await LoadManifestWithEvidenceAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        string diagram = diagramGenerator.GenerateMermaid(manifest);
        string summary = summaryGenerator.GenerateMarkdown(manifest, evidence);

        return Ok(new ManifestBundleResponse
        {
            ManifestVersion = manifestVersion,
            Manifest = manifest,
            Diagram = diagram,
            Summary = summary
        });
    }

    [HttpGet("manifest/{manifestVersion}/export")]
    [ProducesResponseType(typeof(ManifestExportContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestExport(
        [FromRoute] string manifestVersion,
        CancellationToken cancellationToken)
    {
        (GoldenManifest? manifest, AgentEvidencePackage? evidence) = await LoadManifestWithEvidenceAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        string diagram = diagramGenerator.GenerateMermaid(manifest);
        string summary = summaryGenerator.GenerateMarkdown(manifest, evidence);
        string markdown = exportService.GenerateMarkdownPackage(manifest, diagram, summary, evidence);

        return Ok(new ManifestExportContentResponse
        {
            ManifestVersion = manifestVersion,
            Format = FormatMarkdown,
            Content = markdown
        });
    }

    [HttpGet("manifest/{manifestVersion}/export/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadManifestExport(
        [FromRoute] string manifestVersion,
        CancellationToken cancellationToken)
    {
        (GoldenManifest? manifest, AgentEvidencePackage? evidence) = await LoadManifestWithEvidenceAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return this.NotFoundProblem($"Manifest '{manifestVersion}' was not found.", ProblemTypes.ManifestNotFound);

        string diagram = diagramGenerator.GenerateMermaid(manifest);
        string summary = summaryGenerator.GenerateMarkdown(manifest, evidence);
        string markdown = exportService.GenerateMarkdownPackage(manifest, diagram, summary, evidence);

        string fileName = $"architecture-export-{manifestVersion}.md";
        return ApiFileResults.RangeText(Request, markdown, "text/markdown", fileName);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates and loads both manifest versions, then produces their diff.
    /// Returns a non-null <see cref="LoadedManifestPair.Error"/> on any validation or 404 failure.
    /// </summary>
    private async Task<LoadedManifestPair> LoadAndCompareManifestPairAsync(
        string leftVersion,
        string rightVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leftVersion))
            return new LoadedManifestPair { Error = this.BadRequestProblem("leftVersion is required.", ProblemTypes.ValidationFailed) };

        if (string.IsNullOrWhiteSpace(rightVersion))
            return new LoadedManifestPair { Error = this.BadRequestProblem("rightVersion is required.", ProblemTypes.ValidationFailed) };

        GoldenManifest? left = await manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);

        if (left is null)
            return new LoadedManifestPair { Error = this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound) };

        GoldenManifest? right = await manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);

        return right is null ? new LoadedManifestPair { Error = this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound) } : new LoadedManifestPair { Left = left, Right = right, Diff = manifestDiffService.Compare(left, right) };
    }

    /// <summary>
    /// Loads a manifest by version together with its associated evidence package.
    /// Returns <c>(null, null)</c> when the manifest does not exist.
    /// </summary>
    private async Task<(GoldenManifest? Manifest, AgentEvidencePackage? Evidence)> LoadManifestWithEvidenceAsync(
        string manifestVersion,
        CancellationToken cancellationToken)
    {
        GoldenManifest? manifest = await manifestRepository.GetByVersionAsync(manifestVersion, cancellationToken);
        if (manifest is null)
            return (null, null);
        AgentEvidencePackage? evidence = await agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        return (manifest, evidence);
    }

    private sealed class LoadedManifestPair
    {
        public GoldenManifest? Left
        {
            get; init;
        }
        public GoldenManifest? Right
        {
            get; init;
        }
        public ManifestDiffResult? Diff
        {
            get; init;
        }
        public IActionResult? Error
        {
            get; init;
        }
    }
}

