namespace ArchLucid.Core.Configuration;

/// <summary>Dual-pipeline run commit: <see langword="true"/> selects the legacy coordinator merge path; <see langword="false"/>
/// selects the authority <c>IDecisionEngine</c> path (run row must have context, graph, and findings snapshot ids).</summary>
public sealed class LegacyRunCommitPathOptions
{
    public const string SectionName = "Coordinator";

    /// <summary>Legacy coordinator <c>ArchitectureRunCommitOrchestrator</c> when <see langword="true"/>; authority <c>AuthorityDrivenArchitectureRunCommitOrchestrator</c> when <see langword="false"/>.</summary>
    public bool LegacyRunCommitPath
    {
        get; set;
    }
}
