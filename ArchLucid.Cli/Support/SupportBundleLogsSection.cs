using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleLogsSection
{
    [JsonPropertyName("note")]
    public string Note { get; init; } =
        "The CLI bundle does not read API host log files. On the API host, search logs for the structured line "
        + "'Pilot/support configuration snapshot' and use GET /version and GET /health/ready. "
        + "Attach Serilog sink output or container stdout if applicable.";

    [JsonPropertyName("localLogExcerpt")]
    public string? LocalLogExcerpt { get; init; }
}
