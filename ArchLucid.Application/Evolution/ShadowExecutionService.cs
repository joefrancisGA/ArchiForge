using System.Globalization;
using System.Text;

using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Evolution;

/// <summary>
/// Shadow execution: single DB read (<see cref="IRunDetailQueryService.GetRunDetailAsync"/>), then an in-memory-only analysis pass.
/// Does not call replay, determinism, or repository writes; optional manifest compare/agent compare are disabled to avoid extra DB reads.
/// </summary>
public sealed class ShadowExecutionService(
    IRunDetailQueryService runDetailQueryService,
    IArchitectureAnalysisService architectureAnalysisService)
    : IShadowExecutionService
{
    /// <inheritdoc />
    public async Task<ArchitectureAnalysisReport> ExecuteAsync(
        ShadowExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaselineArchitectureRunId);
        ArgumentNullException.ThrowIfNull(request.CandidateChangeSet);

        ArchitectureRunDetail? loaded =
            await runDetailQueryService.GetRunDetailAsync(request.BaselineArchitectureRunId, cancellationToken);

        if (loaded is null)
        {
            throw new RunNotFoundException(request.BaselineArchitectureRunId);
        }

        ArchitectureRunDetail isolated = ArchitectureRunDetailIsolatingCloner.Clone(loaded);

        ApplyCandidateChangeSet(isolated, request.CandidateChangeSet);

        ShadowExecutionPipelineOptions pipeline = request.Pipeline ?? new ShadowExecutionPipelineOptions();

        ArchitectureAnalysisRequest analysisRequest = new()
        {
            RunId = request.BaselineArchitectureRunId,
            PreloadedRunDetail = isolated,
            IncludeEvidence = false,
            IncludeExecutionTraces = false,
            IncludeManifest = pipeline.IncludeManifest,
            IncludeDiagram = pipeline.IncludeDiagram,
            IncludeSummary = pipeline.IncludeSummary,
            IncludeDeterminismCheck = false,
            IncludeManifestCompare = false,
            IncludeAgentResultCompare = false,
        };

        return await architectureAnalysisService.BuildAsync(analysisRequest, cancellationToken);
    }

    private static void ApplyCandidateChangeSet(ArchitectureRunDetail detail, CandidateChangeSet changeSet)
    {
        string annotation = BuildManifestAnnotation(changeSet);

        if (detail.Manifest is not null)
        {
            string prior = detail.Manifest.Metadata.ChangeDescription;

            detail.Manifest.Metadata.ChangeDescription = string.IsNullOrEmpty(prior)
                ? annotation
                : string.Concat(prior, Environment.NewLine, annotation);
        }

        IOrderedEnumerable<CandidateChangeSetStep> orderedSteps = changeSet.ProposedActions
            .OrderBy(static s => s.Ordinal)
            .ThenBy(static s => s.ActionType, StringComparer.Ordinal)
            .ThenBy(static s => s.Description, StringComparer.Ordinal);

        DateTime stamp = DateTime.UtcNow;

        foreach (CandidateChangeSetStep step in orderedSteps)
        {
            RunEventTracePayload payload = new()
            {
                TraceId = Guid.NewGuid().ToString("N"),
                RunId = detail.Run.RunId,
                EventType = "Shadow.CandidateStep",
                EventDescription = string.Format(
                    CultureInfo.InvariantCulture,
                    "[60R] {0}: {1}",
                    step.ActionType,
                    step.Description),
                CreatedUtc = stamp,
                Metadata = { ["ChangeSetId"] = changeSet.ChangeSetId.ToString("D"), ["StepOrdinal"] = step.Ordinal.ToString(CultureInfo.InvariantCulture) }
            };

            if (!string.IsNullOrEmpty(step.AcceptanceCriteria))
            {
                payload.Metadata["AcceptanceCriteria"] = step.AcceptanceCriteria;
            }

            detail.DecisionTraces.Add(RunEventTrace.From(payload));
        }
    }

    private static string BuildManifestAnnotation(CandidateChangeSet changeSet)
    {
        StringBuilder builder = new();
        builder.Append("[60R shadow] CandidateChangeSet ");
        builder.Append(changeSet.ChangeSetId.ToString("D"));

        if (string.IsNullOrWhiteSpace(changeSet.Description))
            return builder.ToString();

        builder.Append(" — ");
        builder.Append(changeSet.Description.Trim());

        return builder.ToString();
    }
}
