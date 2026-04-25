using Microsoft.Extensions.Configuration;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Coordinates SQL RLS break-glass: requires both environment variable and explicit configuration consent.
/// </summary>
public static class RlsBreakGlass
{
    /// <summary>
    ///     True when <c>ARCHLUCID_ALLOW_RLS_BYPASS=true</c> and <c>ArchLucid:Persistence:AllowRlsBypass</c> is true.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? env = Environment.GetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS");
        bool envOk = string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
        bool configOk = configuration.GetValue("ArchLucid:Persistence:AllowRlsBypass", false);

        return envOk && configOk;
    }
}
