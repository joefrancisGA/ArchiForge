using ArchiForge.Api.Auth.Models;
using ArchiForge.Host.Core.Configuration;

namespace ArchiForge.Api.Configuration;

/// <summary>Binds merged auth options (legacy + <c>ArchLucidAuth</c> override) for API startup.</summary>
public static class ArchiForgeAuthConfigurationBridge
{
    /// <inheritdoc cref="ArchiForgeConfigurationBridge.ArchLucidAuthSectionName" />
    public static ArchiForgeAuthOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ArchiForgeAuthOptions options = new();
        configuration.GetSection(ArchiForgeAuthOptions.SectionName).Bind(options);
        IConfigurationSection lucid = configuration.GetSection(ArchiForgeConfigurationBridge.ArchLucidAuthSectionName);

        if (lucid.Exists())
        {
            lucid.Bind(options);
        }

        return options;
    }
}
