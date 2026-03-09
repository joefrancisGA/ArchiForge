using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchiForge.Contracts.Common;

public static class ContractJson
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}