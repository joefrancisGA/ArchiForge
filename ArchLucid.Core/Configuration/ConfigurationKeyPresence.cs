using Microsoft.Extensions.Configuration;

namespace ArchLucid.Core.Configuration;

/// <summary>Scalar presence checks aligned with "set vs missing" in <c>archlucid config check</c> (no secret material).</summary>
public static class ConfigurationKeyPresence
{
  public static bool IsValuePresent(IConfiguration configuration, string configPath)
  {
    ArgumentNullException.ThrowIfNull(configuration);
    if (string.IsNullOrWhiteSpace(configPath))
      return false;

    string? v = configuration[configPath];
    if (v is null)
      return false;

    if (string.IsNullOrWhiteSpace(v))
      return false;

    return true;
  }
}
