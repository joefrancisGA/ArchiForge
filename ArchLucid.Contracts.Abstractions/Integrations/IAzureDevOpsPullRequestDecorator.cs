namespace ArchLucid.Contracts.Abstractions.Integrations;

/// <summary>Posts ArchLucid commit evidence to an Azure DevOps PR (status + thread comment).</summary>
public interface IAzureDevOpsPullRequestDecorator
{
    /// <summary>
    ///     After a golden manifest commit, posts a PR status and a thread comment summarizing the run/manifest ids.
    /// </summary>
    Task PostManifestDeltaAsync(
        Guid goldenManifestId,
        Guid runId,
        AzureDevOpsPullRequestTarget target,
        CancellationToken cancellationToken);
}
