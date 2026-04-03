namespace ArchiForge.Api.Hosting;

/// <summary>Reads <c>Hosting:Role</c> from configuration (env <c>Hosting__Role</c>).</summary>
public static class HostingRoleResolver
{
    private const string ConfigurationKey = "Hosting:Role";

    /// <summary>
    /// Returns <see cref="ArchiForgeHostingRole.Combined"/> when missing or unrecognized (backward compatible).
    /// </summary>
    public static ArchiForgeHostingRole Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? raw = configuration[ConfigurationKey]?.Trim();

        if (string.IsNullOrEmpty(raw))
            return ArchiForgeHostingRole.Combined;

        if (string.Equals(raw, "Api", StringComparison.OrdinalIgnoreCase))
            return ArchiForgeHostingRole.Api;

        if (string.Equals(raw, "Worker", StringComparison.OrdinalIgnoreCase))
            return ArchiForgeHostingRole.Worker;

        if (string.Equals(raw, "Combined", StringComparison.OrdinalIgnoreCase))
            return ArchiForgeHostingRole.Combined;

        return ArchiForgeHostingRole.Combined;
    }
}
