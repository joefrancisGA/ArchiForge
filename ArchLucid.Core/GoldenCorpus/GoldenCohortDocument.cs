using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Core.GoldenCorpus;

/// <summary>Root JSON for <c>tests/golden-cohort/cohort.json</c>.</summary>
public sealed class GoldenCohortDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion
    {
        get;
        set;
    }

    [JsonPropertyName("cohortName")]
    public string CohortName
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("description")]
    public string Description
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("items")]
    public List<GoldenCohortItem> Items
    {
        get;
        set;
    } = [];

    public static GoldenCohortDocument Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        string json = File.ReadAllText(path);

        GoldenCohortDocument? doc = JsonSerializer.Deserialize<GoldenCohortDocument>(json, SerializerOptions);

        return doc ?? throw new InvalidOperationException($"Failed to deserialize cohort JSON: {path}");
    }

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        string json = JsonSerializer.Serialize(this, SerializerOptions);
        string? dir = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, json);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
    };
}

/// <summary>One cohort scenario row.</summary>
public sealed class GoldenCohortItem
{
    [JsonPropertyName("id")]
    public string Id
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("title")]
    public string Title
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("expectedCommittedManifestSha256")]
    public string ExpectedCommittedManifestSha256
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("expectedFindingCategories")]
    public List<string> ExpectedFindingCategories
    {
        get;
        set;
    } = [];
}
