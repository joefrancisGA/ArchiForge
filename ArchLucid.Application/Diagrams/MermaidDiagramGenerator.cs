using System.Text;

using ArchLucid.Application.Manifests;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Diagrams;

/// <summary>
///     Fixed-layout Mermaid diagram generator that uses opinionated defaults (LR flowchart, runtime-platform
///     node labels, relationship-type edge labels). For configurable rendering use
///     <see cref="ManifestDiagramService" /> with <see cref="ManifestDiagramOptions" /> instead.
/// </summary>
public sealed class MermaidDiagramGenerator : IDiagramGenerator
{
    /// <inheritdoc />
    public string GenerateMermaid(GoldenManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        StringBuilder sb = new();

        sb.AppendLine("flowchart LR");

        string servicesBlock = string.Join(Environment.NewLine, manifest.Services.OrderBy(s => s.ServiceName)
            .Select(service =>
                $"    {SanitizeId(service.ServiceId)}[{EscapeLabel(BuildServiceLabel(service))}]"));

        if (servicesBlock.Length > 0)
            sb.AppendLine(servicesBlock);

        string datastoreBlock = string.Join(Environment.NewLine, manifest.Datastores.OrderBy(d => d.DatastoreName)
            .Select(datastore =>
                $"    {SanitizeId(datastore.DatastoreId)}[(\"{EscapeLabel(BuildDatastoreLabel(datastore))}\")]"));

        if (datastoreBlock.Length > 0)
            sb.AppendLine(datastoreBlock);

        string relationshipBlock = string.Join(Environment.NewLine, manifest.Relationships.OrderBy(r => r.SourceId)
            .ThenBy(r => r.TargetId)
            .Select(relationship =>
            {
                string source = SanitizeId(relationship.SourceId);
                string target = SanitizeId(relationship.TargetId);
                string label = EscapeLabel(BuildRelationshipLabel(relationship));

                return $"    {source} -->|{label}| {target}";
            }));

        if (relationshipBlock.Length > 0)
            sb.AppendLine(relationshipBlock);

        return sb.ToString();
    }

    /// <summary>Returns a two-line label: service name + runtime platform.</summary>
    private static string BuildServiceLabel(ManifestService service)
    {
        return $"{service.ServiceName}\\n{service.RuntimePlatform}";
    }

    /// <summary>Returns a multi-line label: datastore name + runtime platform + optional private-endpoint flag.</summary>
    private static string BuildDatastoreLabel(ManifestDatastore datastore)
    {
        string suffix = datastore.PrivateEndpointRequired ? "\\nPrivate Endpoint" : string.Empty;
        return $"{datastore.DatastoreName}\\n{datastore.RuntimePlatform}{suffix}";
    }

    /// <summary>Returns the terse edge label via <see cref="ManifestPresentation.RelationshipLabel" />.</summary>
    private static string BuildRelationshipLabel(ManifestRelationship relationship)
    {
        return ManifestPresentation.RelationshipLabel(relationship.RelationshipType);
    }

    private static string SanitizeId(string value)
    {
        return DiagramIdSanitizer.Sanitize(value);
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
