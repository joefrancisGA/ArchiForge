namespace ArchLucid.Cli.Real;

/// <summary>Resolves which compose overlay files to apply after <c>docker-compose.yml</c>.</summary>
internal static class ComposeOverlayResolver
{
    /// <summary>Returns ordered overlay file names relative to the repo root (excluding <c>docker-compose.yml</c>).</summary>
    public static IReadOnlyList<string> Resolve(bool realMode)
    {
        if (realMode)
            return ["docker-compose.demo.yml", "docker-compose.real-aoai.yml"];


        return ["docker-compose.demo.yml"];
    }
}
