using System.Text;

using ArchLucid.Application.Evidence;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Summaries;

/// <summary>
///     Generates a narrative Markdown summary of a <see cref="GoldenManifest" />, optionally followed
///     by an evidence context section when an <see cref="AgentEvidencePackage" /> is supplied.
///     Intended for LLM-facing and export-facing surfaces.
/// </summary>
public sealed class MarkdownManifestSummaryGenerator(IEvidenceSummaryFormatter evidenceFormatter)
    : IManifestSummaryGenerator
{
    /// <inheritdoc />
    public string GenerateMarkdown(
        GoldenManifest manifest,
        AgentEvidencePackage? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        List<ManifestService> services = manifest.Services;
        List<ManifestDatastore> datastores = manifest.Datastores;
        List<ManifestRelationship> relationships = manifest.Relationships;

        StringBuilder sb = new();

        AppendOverview(sb, manifest, services, datastores, relationships);
        AppendServices(sb, services);
        AppendDatastores(sb, datastores);
        AppendRelationships(sb, relationships);
        AppendGovernance(sb, manifest.Governance);
        AppendMetadata(sb, manifest.Metadata);

        if (evidence is null)
            return sb.ToString();

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(evidenceFormatter.FormatMarkdown(evidence).Trim());
        sb.AppendLine();

        return sb.ToString();
    }

    private static void AppendOverview(
        StringBuilder sb,
        GoldenManifest manifest,
        IReadOnlyList<ManifestService> services,
        IReadOnlyList<ManifestDatastore> datastores,
        IReadOnlyList<ManifestRelationship> relationships)
    {
        sb.AppendLine($"# Architecture Summary: {manifest.SystemName}");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine(
            $"{manifest.SystemName} is represented by a GoldenManifest containing " +
            $"{services.Count} service(s), {datastores.Count} datastore(s), " +
            $"and {relationships.Count} relationship(s).");
        sb.AppendLine();
    }

    private static void AppendServices(StringBuilder sb, IReadOnlyList<ManifestService> services)
    {
        if (services.Count == 0)
            return;

        sb.AppendLine("## Services");
        sb.AppendLine();

        foreach (ManifestService service in services.OrderBy(s => s.ServiceName))
        {
            sb.AppendLine($"- **{service.ServiceName}**");
            sb.AppendLine($"  - Type: {service.ServiceType}");
            sb.AppendLine($"  - Platform: {service.RuntimePlatform}");

            if (!string.IsNullOrWhiteSpace(service.Purpose))
                sb.AppendLine($"  - Purpose: {service.Purpose}");

            List<string> controls = service.RequiredControls;
            if (controls.Count > 0)
                sb.AppendLine($"  - Required Controls: {string.Join(", ", controls)}");

            List<string> tags = service.Tags;

            if (tags.Count > 0)
                sb.AppendLine($"  - Tags: {string.Join(", ", tags)}");
        }

        sb.AppendLine();
    }

    private static void AppendDatastores(StringBuilder sb, IReadOnlyList<ManifestDatastore> datastores)
    {
        if (datastores.Count == 0)
            return;

        sb.AppendLine("## Datastores");
        sb.AppendLine();

        foreach (ManifestDatastore datastore in datastores.OrderBy(d => d.DatastoreName))
        {
            sb.AppendLine($"- **{datastore.DatastoreName}**");
            sb.AppendLine($"  - Type: {datastore.DatastoreType}");
            sb.AppendLine($"  - Platform: {datastore.RuntimePlatform}");

            if (!string.IsNullOrWhiteSpace(datastore.Purpose))
                sb.AppendLine($"  - Purpose: {datastore.Purpose}");

            sb.AppendLine($"  - Private Endpoint Required: {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
            sb.AppendLine($"  - Encryption At Rest Required: {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
        }

        sb.AppendLine();
    }

    private static void AppendRelationships(StringBuilder sb, IReadOnlyList<ManifestRelationship> relationships)
    {
        if (relationships.Count == 0)
            return;

        sb.AppendLine("## Relationships");
        sb.AppendLine();

        foreach (ManifestRelationship r in relationships.OrderBy(r => r.SourceId).ThenBy(r => r.TargetId))
        {
            sb.AppendLine($"- **{r.SourceId}** {FormatRelationshipType(r)} **{r.TargetId}**");

            if (!string.IsNullOrWhiteSpace(r.Description))
                sb.AppendLine($"  - {r.Description}");
        }

        sb.AppendLine();
    }

    private static void AppendGovernance(StringBuilder sb, ManifestGovernance? governance)
    {
        sb.AppendLine("## Governance");
        sb.AppendLine();

        List<string> controls = governance?.RequiredControls ?? [];
        sb.AppendLine(controls.Count > 0
            ? $"- Required Controls: {string.Join(", ", controls)}"
            : "- Required Controls: None recorded");

        List<string> tags = governance?.ComplianceTags ?? [];
        sb.AppendLine(tags.Count > 0
            ? $"- Compliance Tags: {string.Join(", ", tags)}"
            : "- Compliance Tags: None recorded");

        List<string> constraints = governance?.PolicyConstraints ?? [];
        sb.AppendLine(constraints.Count > 0
            ? $"- Policy Constraints: {string.Join(", ", constraints)}"
            : "- Policy Constraints: None recorded");

        sb.AppendLine($"- Risk Classification: {governance?.RiskClassification}");
        sb.AppendLine($"- Cost Classification: {governance?.CostClassification}");
        sb.AppendLine();
    }

    private static void AppendMetadata(StringBuilder sb, ManifestMetadata? metadata)
    {
        sb.AppendLine("## Metadata");
        sb.AppendLine();
        sb.AppendLine($"- Manifest Version: {metadata?.ManifestVersion}");

        if (!string.IsNullOrWhiteSpace(metadata?.ParentManifestVersion))
            sb.AppendLine($"- Parent Manifest Version: {metadata.ParentManifestVersion}");

        if (!string.IsNullOrWhiteSpace(metadata?.ChangeDescription))
            sb.AppendLine($"- Change Description: {metadata.ChangeDescription}");

        if (metadata is not null)
            sb.AppendLine($"- Created UTC: {metadata.CreatedUtc:O}");

        List<string> traceIds = metadata?.DecisionTraceIds ?? [];
        if (traceIds.Count > 0)
            sb.AppendLine($"- Decision Trace Count: {traceIds.Count}");
    }

    /// <summary>
    ///     Returns a prose-style label for a relationship (e.g. "reads from", "writes to").
    ///     Intentionally uses fuller prose labels rather than the terse diagram labels in
    ///     <see cref="ArchLucid.Application.Manifests.ManifestPresentation.RelationshipLabel" />.
    /// </summary>
    private static string FormatRelationshipType(ManifestRelationship relationship)
    {
        return relationship.RelationshipType switch
        {
            RelationshipType.Calls => "calls",
            RelationshipType.ReadsFrom => "reads from",
            RelationshipType.WritesTo => "writes to",
            RelationshipType.PublishesTo => "publishes to",
            RelationshipType.SubscribesTo => "subscribes to",
            RelationshipType.AuthenticatesWith => "authenticates with",
            _ => relationship.RelationshipType.ToString()
        };
    }
}
