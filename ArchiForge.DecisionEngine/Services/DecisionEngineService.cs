using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Findings;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.DecisionEngine.Validation;

namespace ArchiForge.DecisionEngine.Services;

public sealed class DecisionEngineService(ISchemaValidationService schemaValidationService) : IDecisionEngineService
{
    private readonly ISchemaValidationService _schemaValidationService = schemaValidationService ?? throw new ArgumentNullException(nameof(schemaValidationService));

    private const string TopicTopologyAcceptance = "TopologyAcceptance";
    private const string TopicSecurityControlPromotion = "SecurityControlPromotion";
    private const string TopicComplexityDisposition = "ComplexityDisposition";
    private const string EventTypeDecisionResolution = "DecisionResolution";
    private const string ControlPrivateEndpoints = "Private Endpoints";
    private const string ControlPrivateNetworking = "Private Networking";
    private const string ControlManagedIdentity = "Managed Identity";

    public DecisionMergeResult MergeResults(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        string? parentManifestVersion = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evaluations);
        ArgumentNullException.ThrowIfNull(decisionNodes);

        DecisionMergeResult output = new DecisionMergeResult();

        if (string.IsNullOrWhiteSpace(runId))
        {
            output.Errors.Add("RunId is required.");
            return output;
        }

        if (string.IsNullOrWhiteSpace(manifestVersion))
        {
            output.Errors.Add("Manifest version is required.");
            return output;
        }

        if (results.Count == 0)
        {
            output.Errors.Add("At least one agent result is required.");
            return output;
        }

        List<AgentResult> validResults = ValidateAndFilterResults(runId, results, output);

        if (output.Errors.Count > 0)
        {
            return output;
        }

        foreach (AgentResult result in validResults)
        {
            string resultJson = SchemaValidationSerializer.Serialize(result);
            SchemaValidationResult schemaValidation = _schemaValidationService.ValidateAgentResultJson(resultJson);

            if (schemaValidation.IsValid)
                continue;

            foreach (string error in schemaValidation.Errors)
            {
                output.Errors.Add($"AgentResult {result.ResultId}: {error}");
            }
        }

        if (output.Errors.Count > 0)
        {
            return output;
        }

        GoldenManifest manifest = CreateBaseManifest(runId, request, manifestVersion, parentManifestVersion);

        MergeAgentResultsIntoManifest(runId, validResults, manifest, output);

        ApplyEvaluationSignals(runId, evaluations, validResults, output);

        ApplyDecisionNodes(runId, decisionNodes, manifest, output);

        ApplyGovernanceDefaults(manifest, request, validResults, output);

        EnsureRequiredControlsAreAppliedToRelevantComponents(manifest, output);

        AttachDecisionTraceIds(manifest, output.DecisionTraces);

        string manifestJson = SchemaValidationSerializer.Serialize(manifest);
        SchemaValidationResult manifestValidation = _schemaValidationService.ValidateGoldenManifestJson(manifestJson);

        if (!manifestValidation.IsValid)
        {
            output.Errors.AddRange(
                manifestValidation.Errors.Select(e => $"GoldenManifest validation failed: {e}"));
            return output;
        }

        output.Manifest = manifest;
        return output;
    }

    private static void ApplyDecisionNodes(
        string runId,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        // Warn on duplicate topics; only the first node per topic is applied.
        foreach (IGrouping<string, DecisionNode> dup in decisionNodes
                     .GroupBy(d => d.Topic, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            output.Warnings.Add(
                $"Decision topic '{dup.Key}' has {dup.Count()} duplicate nodes; only the first will be applied.");
        }

        DecisionNode? topologyAcceptance = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, TopicTopologyAcceptance, StringComparison.OrdinalIgnoreCase));
        if (topologyAcceptance is not null)
        {
            DecisionOption? selected = topologyAcceptance.Options.FirstOrDefault(o => o.OptionId == topologyAcceptance.SelectedOptionId);

            if (selected is null && !string.IsNullOrWhiteSpace(topologyAcceptance.SelectedOptionId))
            {
                output.Errors.Add(
                    $"{TopicTopologyAcceptance} node has SelectedOptionId '{topologyAcceptance.SelectedOptionId}' " +
                    "that does not match any option. Merge aborted to prevent corrupt decision semantics.");
                return;
            }

            if (selected is not null &&
                selected.Description.StartsWith("Reject", StringComparison.OrdinalIgnoreCase))
            {
                output.Errors.Add("Topology proposal was rejected by Decision Engine v2.");
                return;
            }

            AddTrace(
                output,
                runId,
                EventTypeDecisionResolution,
                $"{TopicTopologyAcceptance}: {selected?.Description ?? "Unknown"} | {topologyAcceptance.Rationale}",
                new Dictionary<string, string>
                {
                    ["decisionTopic"] = TopicTopologyAcceptance,
                    ["outcome"] = selected?.Description ?? "Unknown",
                    ["confidence"] = topologyAcceptance.Confidence.ToString("F3")
                });
        }

        DecisionNode? securityPromotion = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, TopicSecurityControlPromotion, StringComparison.OrdinalIgnoreCase));
        if (securityPromotion is not null)
        {
            DecisionOption? selected = securityPromotion.Options.FirstOrDefault(o => o.OptionId == securityPromotion.SelectedOptionId);
            if (selected is not null)
            {
                if (selected.Description.Contains(ControlPrivateEndpoints, StringComparison.OrdinalIgnoreCase) &&
                    !manifest.Governance.RequiredControls.Contains(ControlPrivateEndpoints, StringComparer.OrdinalIgnoreCase))
                {
                    manifest.Governance.RequiredControls.Add(ControlPrivateEndpoints);
                }

                if (selected.Description.Contains(ControlManagedIdentity, StringComparison.OrdinalIgnoreCase) &&
                    !manifest.Governance.RequiredControls.Contains(ControlManagedIdentity, StringComparer.OrdinalIgnoreCase))
                {
                    manifest.Governance.RequiredControls.Add(ControlManagedIdentity);
                }

                AddTrace(
                    output,
                    runId,
                    EventTypeDecisionResolution,
                    $"{TopicSecurityControlPromotion}: {selected.Description} | {securityPromotion.Rationale}",
                    new Dictionary<string, string>
                    {
                        ["decisionTopic"] = TopicSecurityControlPromotion,
                        ["outcome"] = selected.Description,
                        ["confidence"] = securityPromotion.Confidence.ToString("F3")
                    });
            }
        }

        DecisionNode? complexityDecision = decisionNodes.FirstOrDefault(d =>
            string.Equals(d.Topic, TopicComplexityDisposition, StringComparison.OrdinalIgnoreCase));
        if (complexityDecision is null)
            return;

        {
            DecisionOption? selected = complexityDecision.Options.FirstOrDefault(o => o.OptionId == complexityDecision.SelectedOptionId);
            if (selected is not null &&
                selected.Description.Contains("Reduce complexity", StringComparison.OrdinalIgnoreCase))
            {
                manifest.Governance.PolicyConstraints.Add("Review architecture scope for MVP complexity reduction.");
            }

            AddTrace(
                output,
                runId,
                EventTypeDecisionResolution,
                $"{TopicComplexityDisposition}: {selected?.Description ?? "Unknown"} | {complexityDecision.Rationale}",
                new Dictionary<string, string>
                {
                    ["decisionTopic"] = TopicComplexityDisposition,
                    ["outcome"] = selected?.Description ?? "Unknown",
                    ["confidence"] = complexityDecision.Confidence.ToString("F3")
                });
        }
    }

    private static List<AgentResult> ValidateAndFilterResults(
        string runId,
        IReadOnlyCollection<AgentResult> results,
        DecisionMergeResult output)
    {
        List<AgentResult> valid = new List<AgentResult>();

        foreach (AgentResult result in results)
        {
            List<string> errors = ValidateResult(result, runId);
            if (errors.Count > 0)
            {
                output.Errors.AddRange(errors.Select(e => $"AgentResult {result.ResultId}: {e}"));
                continue;
            }

            valid.Add(result);
        }

        return valid;
    }

    private static List<string> ValidateResult(AgentResult result, string runId)
    {
        List<string> errors = new List<string>();

        if (string.IsNullOrWhiteSpace(result.ResultId))
            errors.Add("ResultId is required.");

        if (string.IsNullOrWhiteSpace(result.TaskId))
            errors.Add("TaskId is required.");

        if (string.IsNullOrWhiteSpace(result.RunId))
            errors.Add("RunId is required.");
        else if (!string.Equals(result.RunId, runId, StringComparison.Ordinal))
            errors.Add($"RunId '{result.RunId}' does not match the merge run '{runId}'; cross-run results must not be merged.");

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
#pragma warning disable IDE0305 // Simplify collection initialization
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
                PolicyConstraints = (request.Constraints ?? []).ToList(),
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
#pragma warning restore IDE0305 // Simplify collection initialization
    }

    private static void MergeAgentResultsIntoManifest(
        string runId,
        IReadOnlyCollection<AgentResult> validResults,
        GoldenManifest manifest,
        DecisionMergeResult output)
    {
        foreach (AgentResult result in validResults.OrderBy(r => GetMergeOrder(r.AgentType)))
        {
            AddTrace(
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
        {
            existing.Purpose = incoming.Purpose;
        }

#pragma warning disable IDE0305 // Simplify collection initialization   
        existing.Tags = (existing.Tags ?? [])
            .Union(incoming.Tags ?? [], StringComparer.OrdinalIgnoreCase)
            .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization

        existing.RequiredControls = (existing.RequiredControls ?? []).Union(incoming.RequiredControls ?? [], StringComparer.OrdinalIgnoreCase).ToList();

        AddTrace(
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
        foreach (string control in controls)
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
        foreach (ArchitectureFinding finding in result.Findings ?? [])
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
        if ((request.RequiredCapabilities ?? []).Any(c =>
            c.Contains("private", StringComparison.OrdinalIgnoreCase)))
        {
            AddRequiredControlIfMissing(manifest, ControlPrivateNetworking, output);
        }

        if ((request.RequiredCapabilities ?? []).Any(c =>
            c.Contains("managed identity", StringComparison.OrdinalIgnoreCase)))
        {
            AddRequiredControlIfMissing(manifest, ControlManagedIdentity, output);
        }

        if (validResults.Any(r => r.AgentType == AgentType.Compliance))
        {
#pragma warning disable IDE0305 // Simplify collection initialization
            manifest.Governance.ComplianceTags =
                manifest.Governance.ComplianceTags
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
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
        foreach (string control in manifest.Governance.RequiredControls)
        {
            foreach (ManifestService service in manifest.Services)
            {
                if (!service.RequiredControls.Contains(control, StringComparer.OrdinalIgnoreCase))
                {
                    service.RequiredControls.Add(control);
                }
            }

            if (!control.Equals(ControlPrivateEndpoints, StringComparison.OrdinalIgnoreCase) &&
                !control.Equals(ControlPrivateNetworking, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (ManifestDatastore datastore in manifest.Datastores)
            {
                datastore.PrivateEndpointRequired = true;
            }
        }

        AddTrace(
            output,
            manifest.RunId,
            "GovernanceControlsApplied",
            "Applied governance required controls to relevant manifest components.",
            []);
    }

    private static void AttachDecisionTraceIds(
        GoldenManifest manifest,
        IReadOnlyCollection<DecisionTrace> traces)
    {
#pragma warning disable IDE0305 // Simplify collection initialization
        manifest.Metadata.DecisionTraceIds = traces
            .Select(t => t.TraceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
    }

    /// <summary>
    /// Surfaces evaluation signals in the merge output: adds a trace entry per result
    /// that received at least one evaluation, and promotes a warning when net opposition
    /// is significant (net delta below -0.30).
    /// </summary>
    private static void ApplyEvaluationSignals(
        string runId,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<AgentResult> results,
        DecisionMergeResult output)
    {
        foreach (AgentResult result in results)
        {
            List<AgentEvaluation> taskEvals = evaluations
                .Where(e => e.TargetAgentTaskId == result.TaskId)
                .ToList();

            if (taskEvals.Count == 0)
                continue;

            double netDelta = taskEvals.Sum(e => e.ConfidenceDelta);
            string types = string.Join(", ",
                taskEvals.Select(e => e.EvaluationType).Distinct(StringComparer.OrdinalIgnoreCase));

            AddTrace(
                output,
                runId,
                "EvaluationSignalApplied",
                $"{result.AgentType} received {taskEvals.Count} evaluation(s) " +
                $"(net delta: {netDelta:+0.000;-0.000}): {types}",
                new Dictionary<string, string>
                {
                    ["resultId"] = result.ResultId,
                    ["agentType"] = result.AgentType.ToString(),
                    ["evaluationCount"] = taskEvals.Count.ToString(),
                    ["netConfidenceDelta"] = netDelta.ToString("F3")
                });

            if (netDelta < -0.30)
            {
                output.Warnings.Add(
                    $"{result.AgentType} result '{result.ResultId}' received net opposition " +
                    $"signal ({netDelta:F3}); review decision traces for details.");
            }
        }
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
            Tags = source.Tags.ToList(),
            RequiredControls = source.RequiredControls.ToList()
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
