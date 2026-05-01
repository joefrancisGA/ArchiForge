using ArchLucid.Core.Configuration;

namespace ArchLucid.Api.Configuration;

/// <summary>
/// Copies <c>ArchLucid:ContextIngestion:MaxPayloadBytes</c> to <see cref="ArchitectureRunCreationPayloadLimitsOptions.MaxPayloadBytesKey" />
/// when the new key is unset so deployments keep working. The substring <c>ArchLucid.ContextIngestion</c> must not appear in <c>ArchLucid.Core</c> IL literals
/// (NetArchTest false positive); retaining the legacy spelling only here in the Api host assembly is deliberate.
/// </summary>
internal static class ArchitectureRunCreationConfigurationBridge
{
    private const string LegacyMaxPayloadBytesKey = "ArchLucid:ContextIngestion:MaxPayloadBytes";

    public static void Apply(IConfiguration configuration)
    {
        if (configuration is not ConfigurationManager editable)
            return;

        string? modern = editable[ArchitectureRunCreationPayloadLimitsOptions.MaxPayloadBytesKey]?.Trim();

        if (!string.IsNullOrWhiteSpace(modern))
            return;

        string? legacy = editable[LegacyMaxPayloadBytesKey]?.Trim();

        if (string.IsNullOrWhiteSpace(legacy))
            return;

        editable.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>(
                    ArchitectureRunCreationPayloadLimitsOptions.MaxPayloadBytesKey,
                    legacy)
        ]);
    }
}
