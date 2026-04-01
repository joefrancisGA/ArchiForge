using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleWorkspaceSection
{
    [JsonPropertyName("outputsDirectory")]
    public string? OutputsDirectory { get; init; }

    [JsonPropertyName("outputsExists")]
    public bool OutputsExists { get; init; }

    [JsonPropertyName("fileCount")]
    public int FileCount { get; init; }

    [JsonPropertyName("totalFileBytes")]
    public long TotalFileBytes { get; init; }

    [JsonPropertyName("sampleTopLevelNames")]
    public IReadOnlyList<string> SampleTopLevelNames { get; init; } = [];
}
