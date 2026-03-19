using System.Text;
using ArchiForge.Application.Evidence;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Summaries;

public sealed class MarkdownManifestSummaryGenerator(IEvidenceSummaryFormatter evidenceFormatter)
    : IManifestSummaryGenerator
{
    public string GenerateMarkdown(
        GoldenManifest manifest,
        AgentEvidencePackage? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var sb = new StringBuilder();

        sb.AppendLine($"# Architecture Summary: {manifest.SystemName}");
        sb.AppendLine();

        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine(
            $"{manifest.SystemName} is represented by a GoldenManifest containing " +
            $"{manifest.Services.Count} service(s), {manifest.Datastores.Count} datastore(s), " +
            $"and {manifest.Relationships.Count} relationship(s).");
        sb.AppendLine();

        if (manifest.Services.Count > 0)
        {
            sb.AppendLine("## Services");
            sb.AppendLine();

            foreach (var service in manifest.Services.OrderBy(s => s.ServiceName))
            {
                sb.AppendLine($"- **{service.ServiceName}**");
                sb.AppendLine($"  - Type: {service.ServiceType}");
                sb.AppendLine($"  - Platform: {service.RuntimePlatform}");

                if (!string.IsNullOrWhiteSpace(service.Purpose))
                {
                    sb.AppendLine($"  - Purpose: {service.Purpose}");
                }

                if (service.RequiredControls.Count > 0)
                {
                    sb.AppendLine($"  - Required Controls: {string.Join(", ", service.RequiredControls)}");
                }

                if (service.Tags.Count > 0)
                {
                    sb.AppendLine($"  - Tags: {string.Join(", ", service.Tags)}");
                }
            }

            sb.AppendLine();
        }

        if (manifest.Datastores.Count > 0)
        {
            sb.AppendLine("## Datastores");
            sb.AppendLine();

            foreach (var datastore in manifest.Datastores.OrderBy(d => d.DatastoreName))
            {
                sb.AppendLine($"- **{datastore.DatastoreName}**");
                sb.AppendLine($"  - Type: {datastore.DatastoreType}");
                sb.AppendLine($"  - Platform: {datastore.RuntimePlatform}");

                if (!string.IsNullOrWhiteSpace(datastore.Purpose))
                {
                    sb.AppendLine($"  - Purpose: {datastore.Purpose}");
                }

                sb.AppendLine($"  - Private Endpoint Required: {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
                sb.AppendLine($"  - Encryption At Rest Required: {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
            }

            sb.AppendLine();
        }

        if (manifest.Relationships.Count > 0)
        {
            sb.AppendLine("## Relationships");
            sb.AppendLine();

            foreach (var relationship in manifest.Relationships
                         .OrderBy(r => r.SourceId)
                         .ThenBy(r => r.TargetId))
            {
                sb.AppendLine($"- **{relationship.SourceId}** {FormatRelationship(relationship)} **{relationship.TargetId}**");

                if (!string.IsNullOrWhiteSpace(relationship.Description))
                {
                    sb.AppendLine($"  - {relationship.Description}");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Governance");
        sb.AppendLine();

        if (manifest.Governance.RequiredControls.Count > 0)
        {
            sb.AppendLine($"- Required Controls: {string.Join(", ", manifest.Governance.RequiredControls)}");
        }
        else
        {
            sb.AppendLine("- Required Controls: None recorded");
        }

        if (manifest.Governance.ComplianceTags.Count > 0)
        {
            sb.AppendLine($"- Compliance Tags: {string.Join(", ", manifest.Governance.ComplianceTags)}");
        }
        else
        {
            sb.AppendLine("- Compliance Tags: None recorded");
        }

        if (manifest.Governance.PolicyConstraints.Count > 0)
        {
            sb.AppendLine($"- Policy Constraints: {string.Join(", ", manifest.Governance.PolicyConstraints)}");
        }
        else
        {
            sb.AppendLine("- Policy Constraints: None recorded");
        }

        sb.AppendLine($"- Risk Classification: {manifest.Governance.RiskClassification}");
        sb.AppendLine($"- Cost Classification: {manifest.Governance.CostClassification}");
        sb.AppendLine();

        sb.AppendLine("## Metadata");
        sb.AppendLine();
        sb.AppendLine($"- Manifest Version: {manifest.Metadata.ManifestVersion}");

        if (!string.IsNullOrWhiteSpace(manifest.Metadata.ParentManifestVersion))
        {
            sb.AppendLine($"- Parent Manifest Version: {manifest.Metadata.ParentManifestVersion}");
        }

        if (!string.IsNullOrWhiteSpace(manifest.Metadata.ChangeDescription))
        {
            sb.AppendLine($"- Change Description: {manifest.Metadata.ChangeDescription}");
        }

        sb.AppendLine($"- Created UTC: {manifest.Metadata.CreatedUtc:O}");

        if (manifest.Metadata.DecisionTraceIds.Count > 0)
        {
            sb.AppendLine($"- Decision Trace Count: {manifest.Metadata.DecisionTraceIds.Count}");
        }

        if (evidence is not null)
        {
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(evidenceFormatter.FormatMarkdown(evidence).Trim());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatRelationship(ManifestRelationship relationship)
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
