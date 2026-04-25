using System.Text.Json;

namespace ArchLucid.Persistence.Serialization;

/// <summary>
///     Cached <see cref="JsonSerializerOptions" /> for audit payloads and advisory/orchestration result JSON (CA1869).
/// </summary>
public static class AuditJsonSerializationOptions
{
    public static JsonSerializerOptions Instance
    {
        get;
    } = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
}
