using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Merges validated <see cref="AgentResult" /> proposals (deltas, findings) into a <see cref="GoldenManifest" />.
/// </summary>
public sealed class AgentProposalManifestMerger
{
    public void MergeAgentResultsIntoManifest(
        string runId,
        IReadOnlyCollection<AgentResult> validResults,
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(validResults);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(output);

        foreach (AgentResult result in validResults.OrderBy(r => GetMergeOrder(r.AgentType)))
        {
            DecisionMergeTraceRecorder.AddTrace(
                output,
                runId,
                "AgentResultAccepted",
                $"Accepted {result.AgentType} result with confidence {result.Confidence:F2}.",
                new Dictionary<string, string>
                {
                    ["resultId"] = result.ResultId,
                    ["taskId"] = result.TaskId,
                    ["agentType"] = result.AgentType.ToString()
                });

            if (result.ProposedChanges is not null)
                ApplyProposal(manifest, result.ProposedChanges, output, result.AgentType);

            ApplyFindingsToGovernance(manifest, result, output);
        }
    }

    private static int GetMergeOrder(AgentType agentType)
    {
        return agentType switch
        {
            AgentType.Topology => 10,
            AgentType.Cost => 20,
            AgentType.Compliance => 30,
            AgentType.Critic => 40,
            _ => 100
        };
    }

    private static void ApplyProposal(
        GoldenManifest manifest,
        ManifestDeltaProposal proposal,
        DecisionMergeResult output,
        AgentType agentType)
    {
        MergeServices(manifest, proposal.AddedServices, output, agentType);
        MergeDatastores(manifest, proposal.AddedDatastores, output, agentType);
        MergeRelationships(manifest, proposal.AddedRelationships, output, agentType);
        MergeRequiredControls(manifest, proposal.RequiredControls, output, agentType);
        MergeWarnings(output, proposal.Warnings, agentType);
    }

    private static void MergeServices(
        GoldenManifest manifest,
        IReadOnlyCollection<ManifestService> services,
        DecisionMergeResult output,
        AgentType agentType)
    {
        foreach (ManifestService service in services)
        {
            if (string.IsNullOrWhiteSpace(service.ServiceName))
            {
                output.Warnings.Add($"Skipped unnamed service from {agentType}.");
                continue;
            }

            ManifestService? existing = manifest.Services.FirstOrDefault(s =>
                s.ServiceName.Equals(service.ServiceName, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                manifest.Services.Add(CloneService(service));

                DecisionMergeTraceRecorder.AddTrace(
                    output,
                    manifest.RunId,
                    "ServiceAdded",
                    $"Added service '{service.ServiceName}' from {agentType}.",
                    new Dictionary<string, string>
                    {
                        ["serviceName"] = service.ServiceName,
                        ["agentType"] = agentType.ToString()
                    });

                continue;
            }

            MergeServiceProperties(manifest.RunId, existing, service, output, agentType);
        }
    }

    private static void MergeServiceProperties(
        string runId,
        ManifestService existing,
        ManifestService incoming,
        DecisionMergeResult output,
        AgentType agentType)
    {
        if (string.IsNullOrWhiteSpace(existing.Purpose) && !string.IsNullOrWhiteSpace(incoming.Purpose))
            existing.Purpose = incoming.Purpose;

        existing.Tags = existing.Tags
            .Union(incoming.Tags, StringComparer.OrdinalIgnoreCase)
            .ToList();

        existing.RequiredControls = existing.RequiredControls
            .Union(incoming.RequiredControls, StringComparer.OrdinalIgnoreCase).ToList();

        DecisionMergeTraceRecorder.AddTrace(
            output,
            runId,
            "ServiceMerged",
            $"Merged service '{existing.ServiceName}' from {agentType}.",
            new Dictionary<string, string>
            {
                ["serviceName"] = existing.ServiceName,
                ["agentType"] = agentType.ToString()
            });
    }

    private static void MergeDatastores(
        GoldenManifest manifest,
        IReadOnlyCollection<ManifestDatastore> datastores,
        DecisionMergeResult output,
        AgentType agentType)
    {
        foreach (ManifestDatastore datastore in datastores)
        {
            if (string.IsNullOrWhiteSpace(datastore.DatastoreName))
            {
                output.Warnings.Add($"Skipped unnamed datastore from {agentType}.");
                continue;
            }

            ManifestDatastore? existing = manifest.Datastores.FirstOrDefault(d =>
                d.DatastoreName.Equals(datastore.DatastoreName, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                manifest.Datastores.Add(CloneDatastore(datastore));

                DecisionMergeTraceRecorder.AddTrace(
                    output,
                    manifest.RunId,
                    "DatastoreAdded",
                    $"Added datastore '{datastore.DatastoreName}' from {agentType}.",
                    new Dictionary<string, string>
                    {
                        ["datastoreName"] = datastore.DatastoreName,
                        ["agentType"] = agentType.ToString()
                    });

                continue;
            }

            existing.EncryptionAtRestRequired |= datastore.EncryptionAtRestRequired;
            existing.PrivateEndpointRequired |= datastore.PrivateEndpointRequired;

            if (string.IsNullOrWhiteSpace(existing.Purpose) && !string.IsNullOrWhiteSpace(datastore.Purpose))
                existing.Purpose = datastore.Purpose;

            DecisionMergeTraceRecorder.AddTrace(
                output,
                manifest.RunId,
                "DatastoreMerged",
                $"Merged datastore '{existing.DatastoreName}' from {agentType}.",
                new Dictionary<string, string>
                {
                    ["datastoreName"] = existing.DatastoreName,
                    ["agentType"] = agentType.ToString()
                });
        }
    }

    private static void MergeRelationships(
        GoldenManifest manifest,
        IReadOnlyCollection<ManifestRelationship> relationships,
        DecisionMergeResult output,
        AgentType agentType)
    {
        foreach (ManifestRelationship relationship in relationships)
        {
            if (string.IsNullOrWhiteSpace(relationship.SourceId) || string.IsNullOrWhiteSpace(relationship.TargetId))
            {
                output.Warnings.Add($"Skipped relationship with blank SourceId or TargetId from {agentType}.");
                continue;
            }

            bool duplicate = manifest.Relationships.Any(r =>
                r.SourceId.Equals(relationship.SourceId, StringComparison.OrdinalIgnoreCase) &&
                r.TargetId.Equals(relationship.TargetId, StringComparison.OrdinalIgnoreCase) &&
                r.RelationshipType == relationship.RelationshipType);

            if (duplicate)
                continue;

            manifest.Relationships.Add(CloneRelationship(relationship));

            DecisionMergeTraceRecorder.AddTrace(
                output,
                manifest.RunId,
                "RelationshipAdded",
                $"Added relationship '{relationship.RelationshipType}' from '{relationship.SourceId}' to '{relationship.TargetId}'.",
                new Dictionary<string, string>
                {
                    ["sourceId"] = relationship.SourceId,
                    ["targetId"] = relationship.TargetId,
                    ["relationshipType"] = relationship.RelationshipType.ToString(),
                    ["agentType"] = agentType.ToString()
                });
        }
    }

    private static void MergeRequiredControls(
        GoldenManifest manifest,
        IReadOnlyCollection<string> controls,
        DecisionMergeResult output,
        AgentType agentType)
    {
        foreach (string control in controls)
        {
            if (string.IsNullOrWhiteSpace(control))
                continue;

            if (manifest.Governance.RequiredControls.Any(c =>
                    c.Equals(control, StringComparison.OrdinalIgnoreCase)))
                continue;

            manifest.Governance.RequiredControls.Add(control);

            DecisionMergeTraceRecorder.AddTrace(
                output,
                manifest.RunId,
                "RequiredControlAdded",
                $"Added required control '{control}' from {agentType}.",
                new Dictionary<string, string> { ["control"] = control, ["agentType"] = agentType.ToString() });
        }
    }

    private static void MergeWarnings(
        DecisionMergeResult output,
        IReadOnlyCollection<string> warnings,
        AgentType agentType)
    {
        foreach (string warning in warnings)
        {
            if (string.IsNullOrWhiteSpace(warning))
                continue;

            output.Warnings.Add($"{agentType}: {warning}");
        }
    }

    private static void ApplyFindingsToGovernance(
        GoldenManifest manifest,
        AgentResult result,
        DecisionMergeResult output)
    {
        foreach (ArchitectureFinding finding in result.Findings)
        {
            if (string.Equals(finding.Category, "Compliance", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(finding.Message))

                if (!manifest.Governance.ComplianceTags.Contains(finding.Message, StringComparer.OrdinalIgnoreCase))
                    manifest.Governance.ComplianceTags.Add(finding.Message);

            DecisionMergeTraceRecorder.AddTrace(
                output,
                manifest.RunId,
                "FindingApplied",
                $"Applied finding from {result.AgentType}: {finding.Message}",
                new Dictionary<string, string>
                {
                    ["findingId"] = finding.FindingId,
                    ["agentType"] = result.AgentType.ToString(),
                    ["severity"] = finding.Severity.ToString(),
                    ["category"] = finding.Category
                });
        }
    }

    private static ManifestService CloneService(ManifestService source)
    {
        return new ManifestService
        {
            ServiceId =
                string.IsNullOrWhiteSpace(source.ServiceId) ? Guid.NewGuid().ToString("N") : source.ServiceId,
            ServiceName = source.ServiceName,
            ServiceType = source.ServiceType,
            RuntimePlatform = source.RuntimePlatform,
            Purpose = source.Purpose,
            Tags = source.Tags.ToList(),
            RequiredControls = source.RequiredControls.ToList()
        };
    }

    private static ManifestDatastore CloneDatastore(ManifestDatastore source)
    {
        return new ManifestDatastore
        {
            DatastoreId =
                string.IsNullOrWhiteSpace(source.DatastoreId) ? Guid.NewGuid().ToString("N") : source.DatastoreId,
            DatastoreName = source.DatastoreName,
            DatastoreType = source.DatastoreType,
            RuntimePlatform = source.RuntimePlatform,
            Purpose = source.Purpose,
            PrivateEndpointRequired = source.PrivateEndpointRequired,
            EncryptionAtRestRequired = source.EncryptionAtRestRequired
        };
    }

    private static ManifestRelationship CloneRelationship(ManifestRelationship source)
    {
        return new ManifestRelationship
        {
            RelationshipId =
                string.IsNullOrWhiteSpace(source.RelationshipId)
                    ? Guid.NewGuid().ToString("N")
                    : source.RelationshipId,
            SourceId = source.SourceId,
            TargetId = source.TargetId,
            RelationshipType = source.RelationshipType,
            Description = source.Description
        };
    }
}
