using System.Text;

using ArchiForge.Application.Manifests;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Summaries;

/// <summary>
/// Options-driven Markdown summary of a <see cref="GoldenManifest"/>.
/// Unlike <see cref="MarkdownManifestSummaryGenerator"/>, this service is evidence-agnostic
/// and controlled by <see cref="ManifestSummaryOptions"/> for selective section inclusion.
/// </summary>
public sealed class ManifestSummaryService : IManifestSummaryService
{
    /// <inheritdoc />
    public string GenerateMarkdown(
        GoldenManifest manifest,
        ManifestSummaryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        options ??= ManifestSummaryOptions.Default;

        ManifestGovernance governance = manifest.Governance;
        ManifestMetadata metadata = manifest.Metadata;
        List<ManifestService> services = manifest.Services;
        List<ManifestDatastore> datastores = manifest.Datastores;
        List<ManifestRelationship> relationships = manifest.Relationships;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"# Architecture Summary: {manifest.SystemName}");
        sb.AppendLine();

        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine($"- **System Name:** {manifest.SystemName}");
        sb.AppendLine($"- **Manifest Version:** {metadata.ManifestVersion}");
        sb.AppendLine($"- **Service Count:** {services.Count}");
        sb.AppendLine($"- **Datastore Count:** {datastores.Count}");
        sb.AppendLine($"- **Relationship Count:** {relationships.Count}");
        sb.AppendLine();

        if (options.IncludeRequiredControls && governance.RequiredControls.Count > 0)
        {
            sb.AppendLine("## Required Controls");
            sb.AppendLine();

            foreach (string control in governance.RequiredControls
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"- {control}");
            }

            sb.AppendLine();
        }

        if (services.Count > 0)
        {
            sb.AppendLine("## Services");
            sb.AppendLine();

            foreach (ManifestService service in services.OrderBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"### {service.ServiceName}");
                sb.AppendLine();
                sb.AppendLine($"- **Service Type:** {service.ServiceType}");
                sb.AppendLine($"- **Runtime Platform:** {service.RuntimePlatform}");

                if (!string.IsNullOrWhiteSpace(service.Purpose))
                {
                    sb.AppendLine($"- **Purpose:** {service.Purpose}");
                }

                if (options.IncludeComponentControls && service.RequiredControls.Count > 0)
                {
                    sb.AppendLine($"- **Required Controls:** {string.Join(", ", service.RequiredControls.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
                }

                if (options.IncludeTags && service.Tags.Count > 0)
                {
                    sb.AppendLine($"- **Tags:** {string.Join(", ", service.Tags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
                }

                sb.AppendLine();
            }
        }

        if (datastores.Count > 0)
        {
            sb.AppendLine("## Datastores");
            sb.AppendLine();

            foreach (ManifestDatastore datastore in datastores.OrderBy(d => d.DatastoreName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"### {datastore.DatastoreName}");
                sb.AppendLine();
                sb.AppendLine($"- **Datastore Type:** {datastore.DatastoreType}");
                sb.AppendLine($"- **Runtime Platform:** {datastore.RuntimePlatform}");

                if (!string.IsNullOrWhiteSpace(datastore.Purpose))
                {
                    sb.AppendLine($"- **Purpose:** {datastore.Purpose}");
                }

                sb.AppendLine($"- **Private Endpoint Required:** {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
                sb.AppendLine($"- **Encryption At Rest Required:** {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
                sb.AppendLine();
            }
        }

        if (options.IncludeRelationships && relationships.Count > 0)
        {
            sb.AppendLine("## Relationships");
            sb.AppendLine();

            int max = options.MaxRelationships ?? int.MaxValue;
            foreach (var relationship in relationships
                         .Select(r => new
                         {
                             Relationship = r,
                             SourceName = ManifestPresentation.ResolveComponentName(r.SourceId, manifest),
                             TargetName = ManifestPresentation.ResolveComponentName(r.TargetId, manifest),
                             TypeLabel = ManifestPresentation.RelationshipLabel(r.RelationshipType)
                         })
                         .OrderBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.TargetName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.TypeLabel, StringComparer.OrdinalIgnoreCase)
                         .Take(max))
            {
                sb.AppendLine($"- **{relationship.SourceName}** -> **{relationship.TargetName}** ({relationship.TypeLabel})");

                if (!string.IsNullOrWhiteSpace(relationship.Relationship.Description))
                {
                    sb.AppendLine($"  - {relationship.Relationship.Description}");
                }
            }

            sb.AppendLine();
        }

        if (!options.IncludeComplianceTags || governance.ComplianceTags.Count <= 0) return sb.ToString().TrimEnd();
        
        {
            sb.AppendLine("## Compliance Tags");
            sb.AppendLine();

            foreach (string tag in governance.ComplianceTags
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"- {tag}");
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}

