using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleEnvironmentSection
{
    [JsonPropertyName("machineName")]
    public string MachineName { get; init; } = string.Empty;

    [JsonPropertyName("osDescription")]
    public string OsDescription { get; init; } = string.Empty;

    [JsonPropertyName("osArchitecture")]
    public string OsArchitecture { get; init; } = string.Empty;

    [JsonPropertyName("processArchitecture")]
    public string ProcessArchitecture { get; init; } = string.Empty;

    [JsonPropertyName("dotnetRuntime")]
    public string DotnetRuntime { get; init; } = string.Empty;

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; init; } = string.Empty;

    [JsonPropertyName("archiforgeAndDotnetEnvironment")]
    public IReadOnlyDictionary<string, string> ArchiforgeAndDotnetEnvironment { get; init; } =
        new Dictionary<string, string>();
}
