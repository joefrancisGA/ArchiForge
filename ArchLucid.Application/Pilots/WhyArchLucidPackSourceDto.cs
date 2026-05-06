namespace ArchLucid.Application.Pilots;
/// <summary>
///     Pre-rendered markdown fragments for <see cref = "WhyArchLucidPackBuilder"/> — populated by the API from
///     <c>GET /v1/demo/preview</c> data so Application stays free of <c>ArchLucid.Host.Core.Demo</c> types.
/// </summary>
public sealed record WhyArchLucidPackSourceDto(string RunId, string ProjectId, string ManifestSectionMarkdown, string AuthorityChainSectionMarkdown, string ArtifactsSectionMarkdown, string PipelineTimelineSectionMarkdown, string RunExplanationSectionMarkdown, string CitationsSectionMarkdown, string ComparisonDeltaSampleMarkdown)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(RunId, ProjectId, ManifestSectionMarkdown, AuthorityChainSectionMarkdown, ArtifactsSectionMarkdown, PipelineTimelineSectionMarkdown, RunExplanationSectionMarkdown, CitationsSectionMarkdown, ComparisonDeltaSampleMarkdown);
    private static byte __ValidatePrimaryConstructorArguments(System.String RunId, System.String ProjectId, System.String ManifestSectionMarkdown, System.String AuthorityChainSectionMarkdown, System.String ArtifactsSectionMarkdown, System.String PipelineTimelineSectionMarkdown, System.String RunExplanationSectionMarkdown, System.String CitationsSectionMarkdown, System.String ComparisonDeltaSampleMarkdown)
    {
        ArgumentNullException.ThrowIfNull(RunId);
        ArgumentNullException.ThrowIfNull(ProjectId);
        ArgumentNullException.ThrowIfNull(ManifestSectionMarkdown);
        ArgumentNullException.ThrowIfNull(AuthorityChainSectionMarkdown);
        ArgumentNullException.ThrowIfNull(ArtifactsSectionMarkdown);
        ArgumentNullException.ThrowIfNull(PipelineTimelineSectionMarkdown);
        ArgumentNullException.ThrowIfNull(RunExplanationSectionMarkdown);
        ArgumentNullException.ThrowIfNull(CitationsSectionMarkdown);
        ArgumentNullException.ThrowIfNull(ComparisonDeltaSampleMarkdown);
        return (byte)0;
    }
}