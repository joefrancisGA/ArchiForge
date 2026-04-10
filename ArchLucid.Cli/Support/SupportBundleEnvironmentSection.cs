using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Support;

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

    [JsonPropertyName("archlucidAndDotnetEnvironment")]
    public IReadOnlyDictionary<string, string> ArchlucidAndDotnetEnvironment { get; init; } =
        new Dictionary<string, string>();

}
