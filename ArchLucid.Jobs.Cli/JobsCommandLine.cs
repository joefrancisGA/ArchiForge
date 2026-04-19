namespace ArchLucid.Jobs.Cli;

/// <summary>Parses <c>--job &lt;name&gt;</c> for <see cref="Program"/>.</summary>
internal static class JobsCommandLine
{
    /// <summary>Returns <see langword="false"/> when <paramref name="jobName"/> is missing or whitespace.</summary>
    public static bool TryParseJobName(IReadOnlyList<string> args, out string? jobName, out string? usageError)
    {
        jobName = null;
        usageError = null;

        if (args is null || args.Count == 0)
        {
            usageError = "Required: --job <name>";

            return false;
        }

        for (int i = 0; i < args.Count; i++)
        {
            string token = args[i];

            if (!string.Equals(token, "--job", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Count)
            {
                usageError = "Expected a job name after --job.";

                return false;
            }

            jobName = args[i + 1]?.Trim();

            if (string.IsNullOrWhiteSpace(jobName))
            {
                usageError = "Job name must not be empty.";

                return false;
            }

            return true;
        }

        usageError = "Required: --job <name>";

        return false;
    }
}
