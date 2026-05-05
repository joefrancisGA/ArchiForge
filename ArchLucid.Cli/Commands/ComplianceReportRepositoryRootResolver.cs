namespace ArchLucid.Cli.Commands;

/// <summary>Locates the ArchLucid repository root so <c>docs/security/SOC2_SELF_ASSESSMENT_2026.md</c> can be loaded.</summary>
internal static class ComplianceReportRepositoryRootResolver
{
    internal static readonly string Soc2TemplateRelativePath =
        Path.Combine("docs", "security", "SOC2_SELF_ASSESSMENT_2026.md");

    internal static bool TryResolve(string? explicitRoot, string searchFromDirectory, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? repositoryRoot)
    {
        repositoryRoot = null;

        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            string rooted = Path.GetFullPath(explicitRoot);
            string template = Path.Combine(rooted, Soc2TemplateRelativePath);

            if (!File.Exists(template))
                return false;

            repositoryRoot = rooted;

            return true;
        }

        DirectoryInfo? directory = new(searchFromDirectory);

        for (int ascent = 0; ascent < 32 && directory is not null; ascent++)
        {
            string candidate = Path.Combine(directory.FullName, Soc2TemplateRelativePath);

            if (File.Exists(candidate))
            {
                repositoryRoot = directory.FullName;

                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }
}
