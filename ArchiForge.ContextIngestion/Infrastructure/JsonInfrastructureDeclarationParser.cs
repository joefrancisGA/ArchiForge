using System.Text.Json;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Infrastructure;

public class JsonInfrastructureDeclarationParser : IInfrastructureDeclarationParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool CanParse(string format) =>
        string.Equals(format, "json", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<CanonicalObject>> ParseAsync(
        InfrastructureDeclarationReference declaration,
        CancellationToken ct)
    {
        _ = ct;
        ResourceDeclarationDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<ResourceDeclarationDocument>(declaration.Content, JsonOptions);
        }
        catch (JsonException)
        {
            return Task.FromResult<IReadOnlyList<CanonicalObject>>([]);
        }

        if (doc?.Resources is null || doc.Resources.Count == 0)
            return Task.FromResult<IReadOnlyList<CanonicalObject>>([]);

        var results = new List<CanonicalObject>();

        foreach (var resource in doc.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.Type) || string.IsNullOrWhiteSpace(resource.Name))
                continue;

            var objectType = ResolveObjectType(resource.Type);

            var properties = new Dictionary<string, string>(
                resource.Properties ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(resource.Subtype))
                properties["subtype"] = resource.Subtype!;

            if (!string.IsNullOrWhiteSpace(resource.Region))
                properties["region"] = resource.Region!;

            properties["resourceType"] = resource.Type;

            results.Add(new CanonicalObject
            {
                ObjectType = objectType,
                Name = resource.Name,
                SourceType = "InfrastructureDeclaration",
                SourceId = declaration.DeclarationId,
                Properties = properties
            });
        }

        return Task.FromResult<IReadOnlyList<CanonicalObject>>(results);
    }

    private static string ResolveObjectType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "network" => "TopologyResource",
            "subnet" => "TopologyResource",
            "vnet" => "TopologyResource",
            "storage" => "TopologyResource",
            "compute" => "TopologyResource",
            "appservice" => "TopologyResource",
            "container" => "TopologyResource",
            "database" => "TopologyResource",
            "identity" => "TopologyResource",
            "keyvault" => "SecurityBaseline",
            "firewall" => "SecurityBaseline",
            "nsg" => "SecurityBaseline",
            "policy" => "PolicyControl",
            _ => "TopologyResource"
        };
    }
}
