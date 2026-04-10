using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Support;

public sealed class SupportBundleManifest
{
    [JsonPropertyName("bundleFormatVersion")]
    public string BundleFormatVersion { get; init; } = "1.1";

    [JsonPropertyName("createdUtc")]
    public string CreatedUtc { get; init; } = string.Empty;

    [JsonPropertyName("cliWorkingDirectory")]
    public string CliWorkingDirectory { get; init; } = string.Empty;

    /// <summary>Expected <c>archlucid.json</c> path for this collection (may be absent).</summary>
    [JsonPropertyName("archlucidJsonPath")]
    public string ArchLucidJsonPath { get; init; } = string.Empty;

    [JsonPropertyName("archlucidJsonPresent")]
    public bool ArchLucidJsonPresent { get; init; }

    /// <summary>Suggested file open order for first-line triage (mirrors <c>README.txt</c>).</summary>
    [JsonPropertyName("triageReadOrder")]
    public IReadOnlyList<SupportBundleTriageEntry> TriageReadOrder { get; init; } = [];

    [JsonPropertyName("notes")]
    public string Notes { get; init; } =
        "No secrets, connection strings, or API key values are included. Sensitive env vars appear only as (set)/(not set).";
}
