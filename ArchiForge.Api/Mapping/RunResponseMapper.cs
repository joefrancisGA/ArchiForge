using ArchiForge.Api.Models;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Api.Mapping;

internal static class RunResponseMapper
{
    public static CreateArchitectureRunResponse ToCreateRunResponse(
        ArchitectureRun run,
        EvidenceBundle evidenceBundle,
        IEnumerable<AgentTask> tasks) =>
        new()
        {
            Run = run,
            EvidenceBundle = evidenceBundle,
            Tasks = tasks.ToList()
        };

    public static ExecuteRunResponse ToExecuteRunResponse(
        string runId,
        IEnumerable<AgentResult> results) =>
        new()
        {
            RunId = runId,
            Results = results.ToList()
        };

    public static ReplayRunResponse ToReplayRunResponse(
        string originalRunId,
        string replayRunId,
        string executionMode,
        IEnumerable<AgentResult> results,
        GoldenManifest? manifest,
        IEnumerable<RunEventTrace> decisionTraces,
        IEnumerable<string> warnings) =>
        new()
        {
            OriginalRunId = originalRunId,
            ReplayRunId = replayRunId,
            ExecutionMode = executionMode,
            Results = results.ToList(),
            Manifest = manifest,
            DecisionTraces = decisionTraces.ToList(),
            Warnings = warnings.ToList()
        };

    public static CommitRunResponse ToCommitRunResponse(
        GoldenManifest manifest,
        IEnumerable<RunEventTrace> decisionTraces,
        IEnumerable<string> warnings) =>
        new()
        {
            Manifest = manifest,
            DecisionTraces = decisionTraces.ToList(),
            Warnings = warnings.ToList()
        };

    public static RunDetailsResponse ToRunDetailsResponse(
        ArchitectureRun run,
        List<AgentTask> tasks,
        List<AgentResult> results,
        GoldenManifest? manifest,
        List<RunEventTrace> decisionTraces) =>
        new()
        {
            Run = run,
            Tasks = tasks,
            Results = results,
            Manifest = manifest,
            DecisionTraces = decisionTraces
        };
}
