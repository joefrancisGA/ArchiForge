namespace ArchLucid.Cli.Commands;

/// <summary>Parsed arguments for <c>archlucid compliance-report</c>.</summary>
internal sealed class ComplianceReportOptions
{
    internal ComplianceReportOptions(
        string? outPath,
        string? repoRoot,
        bool withLiveAudit)
    {
        OutPath = outPath;
        RepoRoot = repoRoot;
        WithLiveAudit = withLiveAudit;
    }

    internal string? OutPath
    {
        get;
    }

    internal string? RepoRoot
    {
        get;
    }

    internal bool WithLiveAudit
    {
        get;
    }

    internal static ComplianceReportOptions? Parse(string[] args, out string? error)
    {
        error = null;
        string? outPath = null;
        string? repoRoot = null;
        bool withLiveAudit = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (string.Equals(arg, "--with-live-audit", StringComparison.OrdinalIgnoreCase))
            {
                withLiveAudit = true;

                continue;
            }

            if (string.Equals(arg, "--out", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    error = "Missing value for --out.";

                    return null;
                }

                outPath = args[++i];

                continue;
            }

            if (string.Equals(arg, "--repo", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    error = "Missing value for --repo.";

                    return null;
                }

                repoRoot = args[++i];

                continue;
            }

            if (arg.StartsWith('-'))
            {
                error = $"Unexpected argument: {arg}";

                return null;
            }

            error = $"Unexpected positional argument: {arg}";

            return null;
        }

        return new ComplianceReportOptions(outPath, repoRoot, withLiveAudit);
    }
}
