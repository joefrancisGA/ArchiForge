namespace ArchiForge.Contracts.Architecture;

/// <summary>
/// Well-known <see cref="ArchitectureLinkageNode.Type"/> and <see cref="ArchitectureLinkageEdge.Type"/> values for
/// <see cref="ArchitectureRunProvenanceGraph"/> JSON payloads.
/// </summary>
public static class ArchitectureLinkageKinds
{
    public static class Nodes
    {
        public const string Request = "request";
        public const string Run = "run";
        public const string EvidenceBundle = "evidenceBundle";
        public const string ContextSnapshot = "contextSnapshot";
        public const string GraphSnapshot = "graphSnapshot";
        public const string FindingsSnapshot = "findingsSnapshot";
        public const string GoldenManifestPointer = "goldenManifestPointer";
        public const string ArtifactBundle = "artifactBundle";
        public const string AgentTask = "agentTask";
        public const string AgentResult = "agentResult";
        public const string ArchitectureFinding = "architectureFinding";
        public const string ManifestVersion = "manifestVersion";
        public const string DecisionTrace = "decisionTrace";
        public const string TraceEvent = "traceEvent";
        public const string DecisionNode = "decisionNode";
    }

    public static class Edges
    {
        public const string RequestInitiatedRun = "requestInitiatedRun";
        public const string RunUsesEvidence = "runUsesEvidence";
        public const string RunReferencesSnapshot = "runReferencesSnapshot";
        public const string RunScheduledTask = "runScheduledTask";
        public const string TaskYieldedResult = "taskYieldedResult";
        public const string ResultRaisedFinding = "resultRaisedFinding";
        public const string RunCommittedManifest = "runCommittedManifest";
        public const string ManifestListsTraceId = "manifestListsTraceId";
        public const string TraceContainsEvent = "traceContainsEvent";
        public const string TraceEventFollows = "traceEventFollows";
        public const string RunMaterializedDecision = "runMaterializedDecision";
    }

    public static class Timeline
    {
        public const string RunCreated = "runCreated";
        public const string TaskCreated = "taskCreated";
        public const string TaskCompleted = "taskCompleted";
        public const string ResultRecorded = "resultRecorded";
        public const string TraceEvent = "traceEvent";
        public const string DecisionNodeRecorded = "decisionNodeRecorded";
        public const string ManifestCommitted = "manifestCommitted";
        public const string RunCompleted = "runCompleted";
    }
}
