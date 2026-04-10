using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Support;

/// <summary>Human-oriented hint for which JSON file to open first during triage.</summary>
public sealed class SupportBundleTriageEntry
{
    [JsonPropertyName("file")]
    public string File { get; init; } = string.Empty;

    [JsonPropertyName("why")]
    public string Why { get; init; } = string.Empty;
}
