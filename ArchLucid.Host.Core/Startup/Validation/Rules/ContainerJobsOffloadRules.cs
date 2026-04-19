using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class ContainerJobsOffloadRules
{
    /// <summary>
    /// Ensures every offloaded job slug is declared in <see cref="ArchLucidJobsOptions.DeployedContainerJobNames"/> so
    /// operators cannot disable in-process polling without provisioning the Container Apps Job.
    /// </summary>
    public static void Collect(
        IConfiguration configuration,
        IHostEnvironment environment,
        ArchLucidHostingRole hostingRole,
        List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(errors);

        if (!environment.IsProduction())
        {
            return;
        }

        if (hostingRole != ArchLucidHostingRole.Worker)
        {
            return;
        }

        ArchLucidJobsOptions jobs =
            configuration.GetSection(ArchLucidJobsOptions.SectionPath).Get<ArchLucidJobsOptions>() ?? new ArchLucidJobsOptions();

        if (jobs.OffloadedToContainerJobs is null || jobs.OffloadedToContainerJobs.Length == 0)
        {
            return;
        }

        HashSet<string> deployed = ParseDeployedNames(jobs.DeployedContainerJobNames);

        foreach (string raw in jobs.OffloadedToContainerJobs)
        {
            string name = raw?.Trim() ?? string.Empty;

            if (name.Length == 0)
            {
                continue;
            }

            if (deployed.Contains(name))
            {
                continue;
            }

            errors.Add(
                $"Jobs:OffloadedToContainerJobs includes '{name}' but that slug is not listed in Jobs:DeployedContainerJobNames "
                + "(comma-separated manifest Terraform should set after provisioning azurerm_container_app_job).");
        }
    }

    private static HashSet<string> ParseDeployedNames(string? deployedContainerJobNames)
    {
        HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(deployedContainerJobNames))
        {
            return set;
        }

        foreach (string part in deployedContainerJobNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length > 0)
            {
                set.Add(part);
            }
        }

        return set;
    }
}
