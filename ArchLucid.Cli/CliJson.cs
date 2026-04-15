using System.Text.Json;

namespace ArchLucid.Cli;

internal static class CliJson
{
    private static readonly JsonSerializerOptions CompactCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static void WriteFailureLine(TextWriter writer, int exitCode, string error, string? message = null)
    {
        object payload = message is null
            ? new { ok = false, exitCode, error }
            : new { ok = false, exitCode, error, message };

        writer.WriteLine(JsonSerializer.Serialize(payload, CompactCamelCase));
    }
}
