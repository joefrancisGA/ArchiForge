using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleReferencesSection
{
    [JsonPropertyName("apiEndpoints")]
    public IReadOnlyList<string> ApiEndpoints { get; init; } = [];

    [JsonPropertyName("documentation")]
    public IReadOnlyList<string> Documentation { get; init; } = [];
}
