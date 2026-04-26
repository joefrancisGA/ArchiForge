namespace ArchLucid.Cli.Real;

/// <summary>Builds absolute compose paths for <see cref="Commands.PilotUpCommand" />.</summary>
internal static class ComposePathListBuilder
{
    public static IReadOnlyList<string> BuildAbsolutePaths(string composeDirectory, IReadOnlyList<string> overlayRelativeOrdered)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(composeDirectory);
        ArgumentNullException.ThrowIfNull(overlayRelativeOrdered);

        List<string> list = [Path.Combine(composeDirectory, "docker-compose.yml")];
        list.AddRange(overlayRelativeOrdered.Select(rel => Path.Combine(composeDirectory, rel)));

        return list;
    }
}
