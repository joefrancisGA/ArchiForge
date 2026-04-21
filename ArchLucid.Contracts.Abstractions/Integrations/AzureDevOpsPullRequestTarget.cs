namespace ArchLucid.Contracts.Abstractions.Integrations;

/// <summary>Identifies an Azure DevOps Git pull request for manifest-delta decoration.</summary>
public sealed record AzureDevOpsPullRequestTarget(Guid RepositoryId, int PullRequestId);
