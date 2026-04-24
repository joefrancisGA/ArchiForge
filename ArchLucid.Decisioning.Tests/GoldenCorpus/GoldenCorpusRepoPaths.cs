namespace ArchLucid.Decisioning.Tests.GoldenCorpus;

internal static class GoldenCorpusRepoPaths
{
    internal static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "ArchLucid.sln")))
                return dir;

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not locate ArchLucid.sln (walk AppContext.BaseDirectory parents).");
    }

    /// <summary>Tests read the corpus copied next to the test assembly (CI / dotnet test).</summary>
    internal static string CorpusOutputDirectory =>
        Path.Combine(AppContext.BaseDirectory, "golden-corpus", "decisioning");

    /// <summary>Authoring path under the repo (materializer writes here).</summary>
    internal static string CorpusSourceDirectory =>
        Path.Combine(FindRepoRoot(), "tests", "golden-corpus", "decisioning");
}
