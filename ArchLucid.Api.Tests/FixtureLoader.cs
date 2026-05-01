using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Api.Tests;

public static class FixtureLoader
{
    private static readonly JsonSerializerOptions FixtureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    public static T Load<T>(string relativePath)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "fixtures", relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Fixture file not found: {fullPath}");

        string json = File.ReadAllText(fullPath);

        T? result = JsonSerializer.Deserialize<T>(json, FixtureJsonOptions);

        return result ?? throw new InvalidOperationException($"Failed to deserialize fixture: {relativePath}");
    }
}
