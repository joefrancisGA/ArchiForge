namespace ArchLucid.Application.Pilots;

/// <summary>
/// Pre-rendered markdown fragments for <see cref="WhyArchLucidPackBuilder"/> — populated by the API from
/// <c>GET /v1/demo/preview</c> data so Application stays free of <c>ArchLucid.Host.Core.Demo</c> types.
/// </summary>
public sealed record WhyArchLucidPackSourceDto(
    string RunId,
    string ProjectId,
    string ManifestSectionMarkdown,
    string AuthorityChainSectionMarkdown,
    string ArtifactsSectionMarkdown,
    string PipelineTimelineSectionMarkdown,
    string RunExplanationSectionMarkdown,
    string CitationsSectionMarkdown,
    string ComparisonDeltaSampleMarkdown);
