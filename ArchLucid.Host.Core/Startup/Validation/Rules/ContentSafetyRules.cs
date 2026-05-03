using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class ContentSafetyRules
{
    public static void Collect(IConfiguration configuration, IHostEnvironment environment, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(errors);

        if (HostEnvironmentClassification.IsProductionOrStagingLike(environment, configuration))
        {
            string? endpoint = configuration["ArchLucid:ContentSafety:Endpoint"];
            string? key = configuration["ArchLucid:ContentSafety:ApiKey"];

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))

                errors.Add(
                    "ArchLucid:ContentSafety:Endpoint and ArchLucid:ContentSafety:ApiKey are required when the host is Production or Staging "
                    + "(or when ARCHLUCID_ENVIRONMENT is Production or Staging).");

            return;
        }

        bool enabled = configuration.GetValue("ArchLucid:ContentSafety:Enabled", false);
        bool allowNull = configuration.GetValue("ArchLucid:ContentSafety:AllowNullGuardInDevelopment", true);

        if (!enabled && !allowNull)

            errors.Add(
                "ArchLucid:ContentSafety:AllowNullGuardInDevelopment is false while ArchLucid:ContentSafety:Enabled is false. "
                + "Enable content safety or set AllowNullGuardInDevelopment=true for development.");
    }
}
