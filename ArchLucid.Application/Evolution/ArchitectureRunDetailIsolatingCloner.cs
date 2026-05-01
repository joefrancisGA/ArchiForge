using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Architecture;

namespace ArchLucid.Application.Evolution;

/// <summary>
///     Produces a deep copy of <see cref="ArchitectureRunDetail" /> so shadow overlays do not share mutable graphs with
///     the loaded aggregate.
/// </summary>
internal static class ArchitectureRunDetailIsolatingCloner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static ArchitectureRunDetail Clone(ArchitectureRunDetail source)
    {
        ArgumentNullException.ThrowIfNull(source);

        string json = JsonSerializer.Serialize(source, JsonOptions);

        ArchitectureRunDetail? copy = JsonSerializer.Deserialize<ArchitectureRunDetail>(json, JsonOptions);

        return copy
               ?? throw new InvalidOperationException("Failed to deserialize cloned ArchitectureRunDetail.");
    }
}
