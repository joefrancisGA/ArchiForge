using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.DecisionEngine.Services;

public sealed class DecisionEngineService : IDecisionEngineService
{
    public DecisionMergeResult MergeResults(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        string? parentManifestVersion = null)
    {
        var output = new DecisionMergeResult();

        if (request is null)
        {
            output.Errors.Add("Architecture request is required.");
            return output;
        }

        if (string.IsNullOrWhiteSpace(manifestVersion))
        {
            output.Errors.Add("Manifest version is required.");
            return output;
        }

        if (results is null || results.Count == 0)
        {
            output.Errors.Add("At least one agent result is required.");
            return output;
        }

        evaluations ??= [];
        decisionNodes ??= [];

        var validResults = ValidateAndFilterResults(results, output);

        if (output.Errors.Count > 0)
        {
            return output;
        }

        var manifest = CreateBaseManifest(runId, request, manifestVersion, parentManifestVersion);

        MergeAgentResultsIntoManifest(request, validResults, manifest, output);

        ApplyDecisionNodes(runId, decisionNodes, manifest, output);

        ApplyGovernanceDefaults(manifest, request, validResults, output);

        EnsureRequiredControlsAreAppliedToRelevantComponents(manifest, output);

        AttachDecisionTraceIds(manifest, output.DecisionTraces);

        output.Manifest = manifest;
        return output;
    }

    private static void ApplyDecisionNodes(
        string runId,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        var topologyAcceptance = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, "TopologyAcceptance", StringComparison.OrdinalIgnoreCase));
        if (topologyAcceptance is not null)
        {
            var selected = topologyAcceptance.Options.FirstOrDefault(o => o.OptionId == topologyAcceptance.SelectedOptionId);
            if (selected is not null &&
                selected.Description.StartsWith("Reject", StringComparison.OrdinalIgnoreCase))
            {
                output.Errors.Add("Topology proposal was rejected by Decision Engine v2.");
                return;
            }

            AddTrace(
                output,
                runId,
                "DecisionResolution",
                $"TopologyAcceptance: {selected?.Description ?? "Unknown"} | {topologyAcceptance.Rationale}",
                new Dictionary<string, string>
                {
                    ["decisionTopic"] = "TopologyAcceptance",
                    ["outcome"] = selected?.Description ?? "Unknown",
                    ["confidence"] = topologyAcceptance.Confidence.ToString("F3")
                });
        }

        var securityPromotion = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, "SecurityControlPromotion", StringComparison.OrdinalIgnoreCase));
        if (securityPromotion is not null)
        {
            var selected = securityPromotion.Options.FirstOrDefault(o => o.OptionId == securityPromotion.SelectedOptionId);
            if (selected is not null)
            {
                if (selected.Description.Contains("Private Endpoints", StringComparison.OrdinalIgnoreCase) &&
                    !manifest.Governance.RequiredControls.Contains("Private Endpoints", StringComparer.OrdinalIgnoreCase))
                {
                    manifest.Governance.RequiredControls.Add("Private Endpoints");
                }

                if (selected.Description.Contains("Managed Identity", StringComparison.OrdinalIgnoreCase) &&
                    !manifest.Governance.RequiredControls.Contains("Managed Identity", StringComparer.OrdinalIgnoreCase))
                {
                    manifest.Governance.RequiredControls.Add("Managed Identity");
                }

                AddTrace(
                    output,
                    runId,
                    "DecisionResolution",
                    $"SecurityControlPromotion: {selected.Description} | {securityPromotion.Rationale}",
                    new Dictionary<string, string>
                    {
                        ["decisionTopic"] = "SecurityControlPromotion",
                        ["outcome"] = selected.Description,
                        ["confidence"] = securityPromotion.Confidence.ToString("F3")
                    });
            }
        }

        var complexityDecision = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, "ComplexityDisposition", StringComparison.OrdinalIgnoreCase));
        if (complexityDecision is not null)
        {
            var selected = complexityDecision.Options.FirstOrDefault(o => o.OptionId == complexityDecision.SelectedOptionId);
            if (selected is not null &&
                selected.Description.Contains("Reduce complexity", StringComparison.OrdinalIgnoreCase))
            {
                manifest.Governance.PolicyConstraints.Add("Review architecture scope for MVP complexity reduction.");
            }

            AddTrace(
                output,
                runId,
                "DecisionResolution",
                $"ComplexityDisposition: {selected?.Description ?? "Unknown"} | {complexityDecision.Rationale}",
                new Dictionary<string, string>
                {
                    ["decisionTopic"] = "ComplexityDisposition",
                    ["outcome"] = selected?.Description ?? "Unknown",
                    ["confidence"] = complexityDecision.Confidence.ToString("F3")
                });
        }
    }

    private static List<AgentResult> ValidateAndFilterResults(
        IReadOnlyCollection<AgentResult> results,
        DecisionMergeResult output)
    {
        var valid = new List<AgentResult>();

        foreach (var result in results)
        {
            var errors = ValidateResult(result);
            if (errors.Count > 0)
            {
                output.Errors.AddRange(errors.Select(e => $"AgentResult {result.ResultId}: {e}"));
                continue;
            }

            valid.Add(result);
        }

        return valid;
    }

    private static List<string> ValidateResult(AgentResult result)
    {
        var errors = new List<string>();

        if (result is null)
        {
            errors.Add("Result is null.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(result.ResultId))
            errors.Add("ResultId is required.");

        if (string.IsNullOrWhiteSpace(result.TaskId))
            errors.Add("TaskId is required.");

        if (string.IsNullOrWhiteSpace(result.RunId))
            errors.Add("RunId is required.");

        if (result.Claims is null)
            errors.Add("Claims collection is required.");

        if (result.EvidenceRefs is null)
            errors.Add("EvidenceRefs collection is required.");

        if (result.Confidence < 0.0 || result.Confidence > 1.0)
            errors.Add("Confidence must be between 0 and 1.");

        return errors;
    }

    private static GoldenManifest CreateBaseManifest(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        string? parentManifestVersion)
    {
        return new GoldenManifest
        {
            RunId = runId,
            SystemName = request.SystemName,
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance
            {
                ComplianceTags = [],
                PolicyConstraints = [.. request.Constraints],
                RequiredControls = [],
                RiskClassification = "Moderate",
                CostClassification = "Moderate"
            },
            Metadata = new ManifestMetadata
            {
                ManifestVersion = manifestVersion,
                ParentManifestVersion = parentManifestVersion,
                ChangeDescription = $"Merged manifest for run {runId}",
                DecisionTraceIds = [],
                CreatedUtc = DateTime.UtcNow
            }
        };
    }

    private static void MergeAgentResultsIntoManifest(
        ArchitectureRequest request,
        IReadOnlyCollection<AgentResult> validResults,
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        foreach (var result in validResults.OrderBy(r => GetMergeOrder(r.AgentType)))
        {
            AddTrace(
                output,
                request.RequestId,
                "AgentResultAccepted",
                $"Accepted {result.AgentType} result with confidence {result.Confidence:F2}.",
                new Dictionary<string, string>
                {
                    ["resultId"] = result.ResultId,
                    ["taskId"] = result.TaskId,
                    ["agentType"] = result.AgentType.ToString()
                });

            if (result.ProposedChanges is not null)
            {
                ApplyProposal(manifest, result.ProposedChanges, output, result.AgentType);
            }

            ApplyFindingsToGovernance(manifest, result, output);
        }
    }

    private static int GetMergeOrder(AgentType agentType) =>
        agentType switch
        {
            AgentType.Topology => 10,
            AgentType.Cost => 20,
            AgentType.Compliance => 30,
            AgentType.Critic => 40,
            _ => 100
        };

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
        foreach (var service in services ?? [])
        {
            if (string.IsNullOrWhiteSpace(service.ServiceName))
            {
                output.Warnings.Add($"Skipped unnamed service from {agentType}.");
                continue;
            }

            var existing = manifest.Services.FirstOrDefault(s =>
                s.ServiceName.Equals(service.ServiceName, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                manifest.Services.Add(CloneService(service));

                AddTrace(
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

            MergeServiceProperties(existing, service, output, agentType);
        }
    }

    private static void MergeServiceProperties(
        ManifestService existing,
        ManifestService incoming,
        DecisionMergeResult output,
        AgentType agentType)
    {
        if (string.IsNullOrWhiteSpace(existing.Purpose) && !string.IsNullOrWhiteSpace(incoming.Purpose))
        {
            existing.Purpose = incoming.Purpose;
        }

        existing.Tags = existing.Tags
            .Union(incoming.Tags ?? [], StringComparer.OrdinalIgnoreCase)
            .ToList();

        existing.RequiredControls = [.. existing.RequiredControls.Union(incoming.RequiredControls ?? [], StringComparer.OrdinalIgnoreCase)];

        AddTrace(
            output,
            string.Empty,
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
        foreach (var datastore in datastores ?? [])
        {
            if (string.IsNullOrWhiteSpace(datastore.DatastoreName))
            {
                output.Warnings.Add($"Skipped unnamed datastore from {agentType}.");
                continue;
            }

            var existing = manifest.Datastores.FirstOrDefault(d =>
                d.DatastoreName.Equals(datastore.DatastoreName, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                manifest.Datastores.Add(CloneDatastore(datastore));

                AddTrace(
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
            {
                existing.Purpose = datastore.Purpose;
            }

            AddTrace(
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
        foreach (var relationship in relationships ?? [])
        {
            var duplicate = manifest.Relationships.Any(r =>
                r.SourceId.Equals(relationship.SourceId, StringComparison.OrdinalIgnoreCase) &&
                r.TargetId.Equals(relationship.TargetId, StringComparison.OrdinalIgnoreCase) &&
                r.RelationshipType == relationship.RelationshipType);

            if (duplicate)
            {
                continue;
            }

            manifest.Relationships.Add(CloneRelationship(relationship));

            AddTrace(
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
        foreach (var control in controls ?? [])
        {
            if (string.IsNullOrWhiteSpace(control))
                continue;

            if (manifest.Governance.RequiredControls.Any(c =>
                c.Equals(control, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            manifest.Governance.RequiredControls.Add(control);

            AddTrace(
                output,
                manifest.RunId,
                "RequiredControlAdded",
                $"Added required control '{control}' from {agentType}.",
                new Dictionary<string, string>
                {
                    ["control"] = control,
                    ["agentType"] = agentType.ToString()
                });
        }
    }

    private static void MergeWarnings(
        DecisionMergeResult output,
        IReadOnlyCollection<string> warnings,
        AgentType agentType)
    {
        foreach (var warning in warnings ?? [])
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
        foreach (var finding in result.Findings ?? [])
        {
            if (string.Equals(finding.Category, "Compliance", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(finding.Message))
            {
                if (!manifest.Governance.ComplianceTags.Contains(finding.Message, StringComparer.OrdinalIgnoreCase))
                {
                    manifest.Governance.ComplianceTags.Add(finding.Message);
                }
            }

            AddTrace(
                output,
                manifest.RunId,
                "FindingApplied",
                $"Applied finding from {result.AgentType}: {finding.Message}",
                new Dictionary<string, string>
                {
                    ["findingId"] = finding.FindingId,
                    ["agentType"] = result.AgentType.ToString(),
                    ["severity"] = finding.Severity,
                    ["category"] = finding.Category
                });
        }
    }

    private static void ApplyGovernanceDefaults(
        GoldenManifest manifest,
        ArchitectureRequest request,
        IReadOnlyCollection<AgentResult> validResults,
        DecisionMergeResult output)
    {
        if (request.RequiredCapabilities.Any(c =>
            c.Contains("private", StringComparison.OrdinalIgnoreCase)))
        {
            AddRequiredControlIfMissing(manifest, "Private Networking", output);
        }

        if (request.RequiredCapabilities.Any(c =>
            c.Contains("managed identity", StringComparison.OrdinalIgnoreCase)))
        {
            AddRequiredControlIfMissing(manifest, "Managed Identity", output);
        }

        if (validResults.Any(r => r.AgentType == AgentType.Compliance))
        {
            manifest.Governance.ComplianceTags =
                manifest.Governance.ComplianceTags
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }
    }

    private static void AddRequiredControlIfMissing(
        GoldenManifest manifest,
        string control,
        DecisionMergeResult output)
    {
        if (manifest.Governance.RequiredControls.Contains(control, StringComparer.OrdinalIgnoreCase))
            return;

        manifest.Governance.RequiredControls.Add(control);

        AddTrace(
            output,
            manifest.RunId,
            "RequiredControlDefaulted",
            $"Added default required control '{control}'.",
            new Dictionary<string, string>
            {
                ["control"] = control
            });
    }

    private static void EnsureRequiredControlsAreAppliedToRelevantComponents(
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        foreach (var control in manifest.Governance.RequiredControls)
        {
            foreach (var service in manifest.Services)
            {
                if (!service.RequiredControls.Contains(control, StringComparer.OrdinalIgnoreCase))
                {
                    service.RequiredControls.Add(control);
                }
            }

            if (control.Equals("Private Endpoints", StringComparison.OrdinalIgnoreCase) ||
                control.Equals("Private Networking", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var datastore in manifest.Datastores)
                {
                    datastore.PrivateEndpointRequired = true;
                }
            }
        }

        AddTrace(
            output,
            manifest.RunId,
            "GovernanceControlsApplied",
            "Applied governance required controls to relevant manifest components.",
            new Dictionary<string, string>());
    }

    private static void AttachDecisionTraceIds(
        GoldenManifest manifest,
        IReadOnlyCollection<DecisionTrace> traces)
    {
        manifest.Metadata.DecisionTraceIds = traces
            .Select(t => t.TraceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddTrace(
        DecisionMergeResult output,
        string runId,
        string eventType,
        string description,
        Dictionary<string, string> metadata)
    {
        output.DecisionTraces.Add(new DecisionTrace
        {
            TraceId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            EventType = eventType,
            EventDescription = description,
            CreatedUtc = DateTime.UtcNow,
            Metadata = metadata
        });
    }

    private static ManifestService CloneService(ManifestService source) =>
        new()
        {
            ServiceId = string.IsNullOrWhiteSpace(source.ServiceId) ? Guid.NewGuid().ToString("N") : source.ServiceId,
            ServiceName = source.ServiceName,
            ServiceType = source.ServiceType,
            RuntimePlatform = source.RuntimePlatform,
            Purpose = source.Purpose,
            Tags = source.Tags?.ToList() ?? [],
            RequiredControls = source.RequiredControls?.ToList() ?? []
        };

    private static ManifestDatastore CloneDatastore(ManifestDatastore source) =>
        new()
        {
            DatastoreId = string.IsNullOrWhiteSpace(source.DatastoreId) ? Guid.NewGuid().ToString("N") : source.DatastoreId,
            DatastoreName = source.DatastoreName,
            DatastoreType = source.DatastoreType,
            RuntimePlatform = source.RuntimePlatform,
            Purpose = source.Purpose,
            PrivateEndpointRequired = source.PrivateEndpointRequired,
            EncryptionAtRestRequired = source.EncryptionAtRestRequired
        };

    private static ManifestRelationship CloneRelationship(ManifestRelationship source) =>
        new()
        {
            RelationshipId = string.IsNullOrWhiteSpace(source.RelationshipId) ? Guid.NewGuid().ToString("N") : source.RelationshipId,
            SourceId = source.SourceId,
            TargetId = source.TargetId,
            RelationshipType = source.RelationshipType,
            Description = source.Description
        };
}