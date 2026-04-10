using ArchLucid.Api.Auth.Models;

namespace ArchLucid.Api.Configuration;

/// <summary>Binds <c>ArchLucidAuth</c> options for API startup.</summary>
public static class ArchLucidAuthConfigurationBridge
{
    /// <summary>Loads <see cref="ArchLucidAuthOptions"/> from <see cref="ArchLucidAuthOptions.SectionName"/>.</summary>
    public static ArchLucidAuthOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ArchLucidAuthOptions options = new();
        configuration.GetSection(ArchLucidAuthOptions.SectionName).Bind(options);

        return options;
    }
}
