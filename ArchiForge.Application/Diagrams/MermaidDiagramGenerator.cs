using System.Text;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diagrams;

public sealed class MermaidDiagramGenerator : IDiagramGenerator
{
    public string GenerateMermaid(GoldenManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var sb = new StringBuilder();

        sb.AppendLine("flowchart TD");

        foreach (var service in manifest.Services.OrderBy(s => s.ServiceName))
        {
            sb.AppendLine($"    {SanitizeId(service.ServiceId)}[{EscapeLabel(BuildServiceLabel(service))}]");
        }

        foreach (var datastore in manifest.Datastores.OrderBy(d => d.DatastoreName))
        {
            sb.AppendLine($"    {SanitizeId(datastore.DatastoreId)}[(\"{EscapeLabel(BuildDatastoreLabel(datastore))}\")]");
        }

        foreach (var relationship in manifest.Relationships.OrderBy(r => r.SourceId).ThenBy(r => r.TargetId))
        {
            var source = SanitizeId(relationship.SourceId);
            var target = SanitizeId(relationship.TargetId);
            var label = EscapeLabel(BuildRelationshipLabel(relationship));

            sb.AppendLine($"    {source} -->|{label}| {target}");
        }

        if (manifest.Governance.RequiredControls.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    subgraph GovernanceControls[Governance Controls]");

            for (var i = 0; i < manifest.Governance.RequiredControls.Count; i++)
            {
                var controlId = $"gov_{i + 1}";
                var control = EscapeLabel(manifest.Governance.RequiredControls[i]);
                sb.AppendLine($"        {controlId}[{control}]");
            }

            sb.AppendLine("    end");
        }

        return sb.ToString();
    }

    private static string BuildServiceLabel(ManifestService service)
    {
        return $"{service.ServiceName}\\n{service.RuntimePlatform}";
    }

    private static string BuildDatastoreLabel(ManifestDatastore datastore)
    {
        var suffix = datastore.PrivateEndpointRequired ? "\\nPrivate Endpoint" : string.Empty;
        return $"{datastore.DatastoreName}\\n{datastore.RuntimePlatform}{suffix}";
    }

    private static string BuildRelationshipLabel(ManifestRelationship relationship)
    {
        return relationship.RelationshipType switch
        {
            RelationshipType.Calls => "calls",
            RelationshipType.ReadsFrom => "reads",
            RelationshipType.WritesTo => "writes",
            RelationshipType.PublishesTo => "publishes",
            RelationshipType.SubscribesTo => "subscribes",
            RelationshipType.AuthenticatesWith => "auth",
            _ => relationship.RelationshipType.ToString()
        };
    }

    private static string SanitizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"node_{Guid.NewGuid():N}";
        }

        var chars = value
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray();

        var result = new string(chars);

        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = $"n_{result}";
        }

        return result;
    }

    private static string EscapeLabel(string value)
    {
        return value
            .Replace("\"", "'")
            .Replace("[", "(")
            .Replace("]", ")")
            .Replace("{", "(")
            .Replace("}", ")");
    }
}
