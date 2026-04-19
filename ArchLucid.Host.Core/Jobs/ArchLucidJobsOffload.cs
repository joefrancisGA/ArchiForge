namespace ArchLucid.Host.Core.Jobs;

/// <summary>Reads <see cref="ArchLucidJobsOptions"/> to decide whether in-process hosted services stay registered.</summary>
public static class ArchLucidJobsOffload
{
    /// <summary>Returns <see langword="true"/> when <paramref name="jobName"/> is listed under <c>Jobs:OffloadedToContainerJobs</c>.</summary>
    public static bool IsOffloaded(IConfiguration configuration, string jobName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(jobName))
        {
            return false;
        }

        IConfigurationSection section = configuration.GetSection($"{ArchLucidJobsOptions.SectionPath}:OffloadedToContainerJobs");

        foreach (IConfigurationSection child in section.GetChildren())
        {
            string? value = child.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (string.Equals(value.Trim(), jobName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
