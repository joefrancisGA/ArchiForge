using System.Text.Json;

namespace ArchiForge.Application.Jobs;

/// <summary>Shared JSON options for persisting <see cref="BackgroundJobWorkUnit"/>.</summary>
public static class BackgroundJobWorkUnitJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(BackgroundJobWorkUnit workUnit)
    {
        ArgumentNullException.ThrowIfNull(workUnit);

        return JsonSerializer.Serialize<BackgroundJobWorkUnit>(workUnit, Options);
    }

    public static BackgroundJobWorkUnit? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<BackgroundJobWorkUnit>(json, Options);
    }
}
