using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Support;

/// <summary>
/// Lightweight checks that the published HTTP contract endpoint responds (no full OpenAPI snapshot in bundle).
/// </summary>
public sealed class SupportBundleApiContractSection
{
    [JsonPropertyName("microsoftOpenApiV1")]
    public SupportBundleBoundedHttpProbe MicrosoftOpenApiV1 { get; init; } = new();
}
