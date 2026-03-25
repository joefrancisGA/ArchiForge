using System.Text;

using ArchiForge.Application.Manifests;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diagrams;

/// <summary>
/// Fixed-layout Mermaid diagram generator that uses opinionated defaults (LR flowchart, runtime-platform
/// node labels, relationship-type edge labels). For configurable rendering use
/// <see cref="ManifestDiagramService"/> with <see cref="ManifestDiagramOptions"/> instead.
/// </summary>
public sealed class MermaidDiagramGenerator : IDiagramGenerator
{
    /// <inheritdoc />
    public string GenerateMermaid(GoldenManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("flowchart LR");

        foreach (ManifestService service in manifest.Services.OrderBy(s => s.ServiceName))
        {
            sb.AppendLine($"    {SanitizeId(service.ServiceId)}[{EscapeLabel(BuildServiceLabel(service))}]");
        }

        foreach (ManifestDatastore datastore in manifest.Datastores.OrderBy(d => d.DatastoreName))
        {
            sb.AppendLine($"    {SanitizeId(datastore.DatastoreId)}[(\"{EscapeLabel(BuildDatastoreLabel(datastore))}\")]");
        }

        foreach (ManifestRelationship relationship in manifest.Relationships.OrderBy(r => r.SourceId).ThenBy(r => r.TargetId))
        {
            string source = SanitizeId(relationship.SourceId);
            string target = SanitizeId(relationship.TargetId);
            string label = EscapeLabel(BuildRelationshipLabel(relationship));

            sb.AppendLine($"    {source} -->|{label}| {target}");
        }

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

    /// <summary>Returns the terse edge label via <see cref="ManifestPresentation.RelationshipLabel"/>.</summary>
    private static string BuildRelationshipLabel(ManifestRelationship relationship)
    {
        return ManifestPresentation.RelationshipLabel(relationship.RelationshipType);
    }

    private static string SanitizeId(string value) => DiagramIdSanitizer.Sanitize(value);

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
