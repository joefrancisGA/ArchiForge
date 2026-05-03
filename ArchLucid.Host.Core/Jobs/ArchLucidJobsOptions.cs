namespace ArchLucid.Host.Core.Jobs;

/// <summary>Configuration for Container Apps Job offload and deploy-time manifest checks.</summary>
public sealed class ArchLucidJobsOptions
{
    public const string SectionPath = "Jobs";

    /// <summary>Hosted services matching these slugs are not registered in the Worker when offloaded to jobs.</summary>
    public string[] OffloadedToContainerJobs
    {
        get;
        set;
    } = [];

    /// <summary>
    /// Comma-separated list of job slugs that Terraform has provisioned for this environment (e.g. <c>advisory-scan,data-archival</c>).
    /// Production Worker validates every <see cref="OffloadedToContainerJobs"/> entry appears here.
    /// </summary>
    public string? DeployedContainerJobNames
    {
        get;
        set;
    }
}
