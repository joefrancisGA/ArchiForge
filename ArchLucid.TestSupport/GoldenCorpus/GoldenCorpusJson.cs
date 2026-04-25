using System.Text.Json;

namespace ArchLucid.TestSupport.GoldenCorpus;

/// <summary>Shared JSON options for golden corpus files (camelCase, stable formatting).</summary>
public static class GoldenCorpusJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true
    };
}
