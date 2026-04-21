namespace ArchLucid.Cli.Commands;

/// <summary>
/// Locates the ArchLucid git repository root from a starting directory by probing for
/// <c>docs/go-to-market/MARKETPLACE_PUBLICATION.md</c> (stable marker for CLI commands that read repo docs).
/// </summary>
internal static class CliRepositoryRootResolver
{
    internal static string? TryResolveRepositoryRoot(string? startDirectory = null)
    {
        string current = string.IsNullOrWhiteSpace(startDirectory)
            ? Directory.GetCurrentDirectory()
            : startDirectory;

        DirectoryInfo? directory = new(current);

        for (int ascent = 0; ascent < 24 && directory is not null; ascent++)
        {
            string marker = Path.Combine(
                directory.FullName,
                "docs",
                "go-to-market",
                "MARKETPLACE_PUBLICATION.md");

            if (File.Exists(marker))
                return directory.FullName;

            directory = directory.Parent;
        }

        return null;
    }
}
