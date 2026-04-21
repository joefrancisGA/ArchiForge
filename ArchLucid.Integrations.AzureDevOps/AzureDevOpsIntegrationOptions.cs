namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>Configuration for optional PR decoration when the authority run-completed integration event is consumed.</summary>
public sealed class AzureDevOpsIntegrationOptions
{
    public const string SectionName = "AzureDevOps";

    /// <summary>When false, the integration handler no-ops.</summary>
    public bool Enabled { get; set; }

    /// <summary>Azure DevOps organization name (dev.azure.com/{Organization}/...).</summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>Project name (URL segment, may contain spaces — caller must URL-encode when building URIs).</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>PAT with <c>Code (Read &amp; write)</c> for threads and statuses. Use Key Vault reference in production.</summary>
    public string PersonalAccessToken { get; set; } = string.Empty;

    /// <summary>Git repository id (UUID).</summary>
    public Guid RepositoryId { get; set; }

    /// <summary>
    /// Pull request id to decorate for the <strong>server-side</strong> fan-out handler (single fixed PR from config).
    /// For per-PR fan-out driven by the buyer&apos;s pipeline, use the pipeline-side template at
    /// <c>integrations/azure-devops-task-manifest-delta-pr-comment/</c>; this server-side path is for tenants that want
    /// zero pipeline changes and accept a fixed PR id.
    /// </summary>
    public int PullRequestId { get; set; }

    /// <summary>Optional link shown on PR status (e.g. operator run detail URL).</summary>
    public string StatusTargetUrl { get; set; } = string.Empty;
}
