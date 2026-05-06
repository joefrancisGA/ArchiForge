using System.Globalization;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Application.Architecture;
/// <inheritdoc/>
public sealed class ArchitectureRunProvenanceService(IRunDetailQueryService runDetailQueryService, IArchitectureRequestRepository requestRepository, IEvidenceBundleRepository evidenceBundleRepository, IDecisionNodeRepository decisionNodeRepository) : IArchitectureRunProvenanceService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runDetailQueryService, requestRepository, evidenceBundleRepository, decisionNodeRepository);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.IRunDetailQueryService runDetailQueryService, ArchLucid.Persistence.Data.Repositories.IArchitectureRequestRepository requestRepository, ArchLucid.Persistence.Data.Repositories.IEvidenceBundleRepository evidenceBundleRepository, ArchLucid.Persistence.Data.Repositories.IDecisionNodeRepository decisionNodeRepository)
    {
        ArgumentNullException.ThrowIfNull(runDetailQueryService);
        ArgumentNullException.ThrowIfNull(requestRepository);
        ArgumentNullException.ThrowIfNull(evidenceBundleRepository);
        ArgumentNullException.ThrowIfNull(decisionNodeRepository);
        return (byte)0;
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Contracts.Architecture.ArchitectureRunProvenanceGraph?> GetProvenanceAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
            return null;
        if (detail.HasBrokenManifestReference)
            return null;
        ArchitectureRequest? request = await requestRepository.GetByIdAsync(detail.Run.RequestId, cancellationToken);
        EvidenceBundle? bundle = await TryResolveEvidenceBundleAsync(detail, evidenceBundleRepository, cancellationToken);
        IReadOnlyList<DecisionNode> decisionNodes = await decisionNodeRepository.GetByRunIdAsync(runId, cancellationToken);
        return BuildGraph(detail, request, bundle, decisionNodes);
    }

    private static async Task<EvidenceBundle?> TryResolveEvidenceBundleAsync(ArchitectureRunDetail detail, IEvidenceBundleRepository bundles, CancellationToken cancellationToken)
    {
        string? bundleRef = detail.Tasks.Select(t => t.EvidenceBundleRef).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        if (string.IsNullOrWhiteSpace(bundleRef))
            return null;
        return await bundles.GetByIdAsync(bundleRef, cancellationToken);
    }

    private static ArchitectureRunProvenanceGraph BuildGraph(ArchitectureRunDetail detail, ArchitectureRequest? request, EvidenceBundle? evidenceBundle, IReadOnlyList<DecisionNode> decisionNodes)
    {
        ArchitectureRun run = detail.Run;
        ArchitectureRunProvenanceGraph graph = new()
        {
            RunId = run.RunId,
            TraceabilityGaps = [..CommittedManifestTraceabilityRules.GetLinkageGaps(detail)]
        };
        Dictionary<string, ArchitectureLinkageNode> nodes = new(StringComparer.Ordinal);
        List<ArchitectureLinkageEdge> edges = [];
        List<ArchitectureTraceTimelineEntry> timeline = [];
        string requestNodeId = $"request:{run.RequestId}";
        AddNode(new ArchitectureLinkageNode { Id = requestNodeId, Type = ArchitectureLinkageKinds.Nodes.Request, ReferenceId = run.RequestId, Name = request?.SystemName ?? run.RequestId, Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["environment"] = request?.Environment ?? "" } });
        string runNodeId = $"run:{run.RunId}";
        AddNode(new ArchitectureLinkageNode { Id = runNodeId, Type = ArchitectureLinkageKinds.Nodes.Run, ReferenceId = run.RunId, Name = $"Run {run.RunId}", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["status"] = run.Status.ToString(), ["currentManifestVersion"] = run.CurrentManifestVersion ?? "" } });
        AddEdge(ArchitectureLinkageKinds.Edges.RequestInitiatedRun, requestNodeId, runNodeId);
        timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = run.CreatedUtc, Kind = ArchitectureLinkageKinds.Timeline.RunCreated, Label = "Run created", ReferenceId = run.RunId });
        if (evidenceBundle is not null)
        {
            string bundleId = evidenceBundle.EvidenceBundleId;
            string bundleNodeId = $"evidence:{bundleId}";
            AddNode(new ArchitectureLinkageNode { Id = bundleNodeId, Type = ArchitectureLinkageKinds.Nodes.EvidenceBundle, ReferenceId = bundleId, Name = "Evidence bundle", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) });
            AddEdge(ArchitectureLinkageKinds.Edges.RunUsesEvidence, runNodeId, bundleNodeId);
        }

        AddSnapshotNodes(run, runNodeId, AddNode, AddEdge);
        foreach (AgentTask task in detail.Tasks.OrderBy(t => t.CreatedUtc).ThenBy(t => t.TaskId, StringComparer.Ordinal))
        {
            string taskNodeId = $"task:{task.TaskId}";
            AddNode(new ArchitectureLinkageNode { Id = taskNodeId, Type = ArchitectureLinkageKinds.Nodes.AgentTask, ReferenceId = task.TaskId, Name = task.Objective.Length > 120 ? task.Objective[..120] + "…" : task.Objective, Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["agentType"] = task.AgentType.ToString(), ["status"] = task.Status.ToString() } });
            AddEdge(ArchitectureLinkageKinds.Edges.RunScheduledTask, runNodeId, taskNodeId);
            timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = task.CreatedUtc, Kind = ArchitectureLinkageKinds.Timeline.TaskCreated, Label = $"Task scheduled ({task.AgentType})", ReferenceId = task.TaskId });
            if (task.CompletedUtc is { } completed)
                timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = completed, Kind = ArchitectureLinkageKinds.Timeline.TaskCompleted, Label = $"Task completed ({task.AgentType})", ReferenceId = task.TaskId });
        }

        foreach (AgentResult result in detail.Results.OrderBy(r => r.CreatedUtc).ThenBy(r => r.ResultId, StringComparer.Ordinal))
        {
            string resultNodeId = $"result:{result.ResultId}";
            AddNode(new ArchitectureLinkageNode { Id = resultNodeId, Type = ArchitectureLinkageKinds.Nodes.AgentResult, ReferenceId = result.ResultId, Name = $"{result.AgentType} result", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["confidence"] = result.Confidence.ToString("F2", CultureInfo.InvariantCulture), ["taskId"] = result.TaskId } });
            string taskKey = $"task:{result.TaskId}";
            AddEdge(ArchitectureLinkageKinds.Edges.TaskYieldedResult, nodes.ContainsKey(taskKey) ? taskKey : runNodeId, resultNodeId);
            timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = result.CreatedUtc, Kind = ArchitectureLinkageKinds.Timeline.ResultRecorded, Label = $"Agent result recorded ({result.AgentType})", ReferenceId = result.ResultId });
            foreach (ArchitectureFinding finding in result.Findings)
            {
                string findingNodeId = $"finding:{result.ResultId}:{finding.FindingId}";
                AddNode(new ArchitectureLinkageNode { Id = findingNodeId, Type = ArchitectureLinkageKinds.Nodes.ArchitectureFinding, ReferenceId = finding.FindingId, Name = finding.Message.Length > 160 ? finding.Message[..160] + "…" : finding.Message, Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["severity"] = finding.Severity.ToString(), ["category"] = finding.Category, ["sourceAgent"] = finding.SourceAgent.ToString() } });
                AddEdge(ArchitectureLinkageKinds.Edges.ResultRaisedFinding, resultNodeId, findingNodeId);
            }
        }

        GoldenManifest? manifest = detail.Manifest;
        if (manifest is not null)
        {
            string manifestVersion = manifest.Metadata.ManifestVersion;
            string manifestNodeId = $"manifest:{manifestVersion}";
            AddNode(new ArchitectureLinkageNode { Id = manifestNodeId, Type = ArchitectureLinkageKinds.Nodes.ManifestVersion, ReferenceId = manifestVersion, Name = $"Manifest {manifestVersion}", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["systemName"] = manifest.SystemName, ["runId"] = manifest.RunId } });
            AddEdge(ArchitectureLinkageKinds.Edges.RunCommittedManifest, runNodeId, manifestNodeId);
            timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = manifest.Metadata.CreatedUtc, Kind = ArchitectureLinkageKinds.Timeline.ManifestCommitted, Label = $"Manifest committed ({manifestVersion})", ReferenceId = manifestVersion });
        }

        List<RunEventTrace> runEventTraces = detail.DecisionTraces.OfType<RunEventTrace>().OrderBy(t => t.RunEvent.CreatedUtc).ThenBy(t => t.RunEvent.TraceId, StringComparer.Ordinal).ToList();
        string? previousTraceNodeId = null;
        foreach (RunEventTrace trace in runEventTraces)
        {
            RunEventTracePayload ev = trace.RunEvent;
            string traceNodeId = $"trace:{ev.TraceId}";
            AddNode(new ArchitectureLinkageNode { Id = traceNodeId, Type = ArchitectureLinkageKinds.Nodes.TraceEvent, ReferenceId = ev.TraceId, Name = ev.EventType, Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["eventDescription"] = ev.EventDescription, ["eventType"] = ev.EventType } });
            AddEdge(ArchitectureLinkageKinds.Edges.TraceContainsEvent, runNodeId, traceNodeId);
            if (manifest is not null)
            {
                string manifestNodeId = $"manifest:{manifest.Metadata.ManifestVersion}";
                AddEdge(ArchitectureLinkageKinds.Edges.ManifestListsTraceId, manifestNodeId, traceNodeId);
            }

            if (previousTraceNodeId is not null)
                AddEdge(ArchitectureLinkageKinds.Edges.TraceEventFollows, previousTraceNodeId, traceNodeId);
            previousTraceNodeId = traceNodeId;
            timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = ev.CreatedUtc, Kind = ArchitectureLinkageKinds.Timeline.TraceEvent, Label = $"{ev.EventType}: {ev.EventDescription}", ReferenceId = ev.TraceId, Metadata = new Dictionary<string, string>(ev.Metadata, StringComparer.OrdinalIgnoreCase) });
        }

        foreach (DecisionNode decision in decisionNodes.OrderBy(d => d.CreatedUtc).ThenBy(d => d.DecisionId, StringComparer.Ordinal))
        {
            string decisionNodeId = $"decisionNode:{decision.DecisionId}";
            AddNode(new ArchitectureLinkageNode { Id = decisionNodeId, Type = ArchitectureLinkageKinds.Nodes.DecisionNode, ReferenceId = decision.DecisionId, Name = decision.Topic, Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["selectedOptionId"] = decision.SelectedOptionId ?? "", ["confidence"] = decision.Confidence.ToString("F2", CultureInfo.InvariantCulture) } });
            AddEdge(ArchitectureLinkageKinds.Edges.RunMaterializedDecision, runNodeId, decisionNodeId);
            timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = decision.CreatedUtc, Kind = ArchitectureLinkageKinds.Timeline.DecisionNodeRecorded, Label = $"Decision recorded: {decision.Topic}", ReferenceId = decision.DecisionId });
        }

        if (run.CompletedUtc is { } doneUtc)
            timeline.Add(new ArchitectureTraceTimelineEntry { TimestampUtc = doneUtc, Kind = ArchitectureLinkageKinds.Timeline.RunCompleted, Label = $"Run completed ({run.Status})", ReferenceId = run.RunId, Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["status"] = run.Status.ToString() } });
        graph.Nodes = [..nodes.Values];
        graph.Edges = edges;
        graph.Timeline = [..timeline.OrderBy(x => x.TimestampUtc).ThenBy(x => x.ReferenceId, StringComparer.Ordinal)];
        return graph;
        void AddEdge(string type, string fromId, string toId, Dictionary<string, string>? metadata = null)
        {
            edges.Add(new ArchitectureLinkageEdge { Id = Guid.NewGuid().ToString("N"), Type = type, FromNodeId = fromId, ToNodeId = toId, Metadata = metadata is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase) });
        }

        void AddNode(ArchitectureLinkageNode node)
        {
            nodes[node.Id] = node;
        }
    }

    private static void AddSnapshotNodes(ArchitectureRun run, string runNodeId, Action<ArchitectureLinkageNode> addNode, Action<string, string, string, Dictionary<string, string>?> addEdge)
    {
        if (!string.IsNullOrWhiteSpace(run.ContextSnapshotId))
        {
            string id = $"ctx:{run.ContextSnapshotId}";
            addNode(new ArchitectureLinkageNode { Id = id, Type = ArchitectureLinkageKinds.Nodes.ContextSnapshot, ReferenceId = run.ContextSnapshotId, Name = "Context snapshot", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) });
            addEdge(ArchitectureLinkageKinds.Edges.RunReferencesSnapshot, runNodeId, id, null);
        }

        if (run.GraphSnapshotId is { } graphId)
        {
            string id = $"graph:{graphId:N}";
            addNode(new ArchitectureLinkageNode { Id = id, Type = ArchitectureLinkageKinds.Nodes.GraphSnapshot, ReferenceId = graphId.ToString("N"), Name = "Graph snapshot", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) });
            addEdge(ArchitectureLinkageKinds.Edges.RunReferencesSnapshot, runNodeId, id, null);
        }

        if (run.FindingsSnapshotId is { } findingsId)
        {
            string id = $"findings:{findingsId:N}";
            addNode(new ArchitectureLinkageNode { Id = id, Type = ArchitectureLinkageKinds.Nodes.FindingsSnapshot, ReferenceId = findingsId.ToString("N"), Name = "Findings snapshot", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) });
            addEdge(ArchitectureLinkageKinds.Edges.RunReferencesSnapshot, runNodeId, id, null);
        }

        if (run.GoldenManifestId is { } goldenId)
        {
            string id = $"goldenPointer:{goldenId:N}";
            addNode(new ArchitectureLinkageNode { Id = id, Type = ArchitectureLinkageKinds.Nodes.GoldenManifestPointer, ReferenceId = goldenId.ToString("N"), Name = "Golden manifest pointer", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) });
            addEdge(ArchitectureLinkageKinds.Edges.RunReferencesSnapshot, runNodeId, id, null);
        }

        if (run.ArtifactBundleId is not { } artifactBundleId)
            return;
        {
            string id = $"artifactBundle:{artifactBundleId:N}";
            addNode(new ArchitectureLinkageNode { Id = id, Type = ArchitectureLinkageKinds.Nodes.ArtifactBundle, ReferenceId = artifactBundleId.ToString("N"), Name = "Artifact bundle", Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) });
            addEdge(ArchitectureLinkageKinds.Edges.RunReferencesSnapshot, runNodeId, id, null);
        }
    }
}