using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diagrams;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Summaries;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Application.Analysis;

/// <summary>
/// Builds an <see cref="ArchitectureAnalysisReport"/> by orchestrating manifest, evidence, trace,
/// diagram, summary, determinism, and diff sub-services for a given run.
/// </summary>
public sealed class ArchitectureAnalysisService(
    IRunDetailQueryService runDetailQueryService,
    IUnifiedGoldenManifestReader unifiedGoldenManifestReader,
    IAgentEvidencePackageRepository evidenceRepository,
    IAgentExecutionTraceRepository traceRepository,
    IAgentResultRepository resultRepository,
    IDiagramGenerator diagramGenerator,
    IManifestSummaryGenerator summaryGenerator,
    IDeterminismCheckService determinismCheckService,
    IManifestDiffService manifestDiffService,
    IAgentResultDiffService agentResultDiffService)
    : IArchitectureAnalysisService
{
    private const string ExecutionModeCurrent = ExecutionModes.Current;

    /// <inheritdoc />
    public async Task<ArchitectureAnalysisReport> BuildAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ArgumentException.ThrowIfNullOrWhiteSpace(request.RunId);

        ArchitectureRunDetail? primaryDetail = request.PreloadedRunDetail;
        ArchitectureRun run;

        if (primaryDetail is not null)
        {
            if (!string.Equals(primaryDetail.Run.RunId, request.RunId, StringComparison.Ordinal))

                throw new ArgumentException(
                    "PreloadedRunDetail.Run.RunId must match RunId.",
                    nameof(request));


            run = primaryDetail.Run;
        }
        else if (request.PreloadedRun is not null)

            run = request.PreloadedRun;

        else
        {
            primaryDetail = await runDetailQueryService.GetRunDetailAsync(request.RunId, cancellationToken)
                ?? throw new RunNotFoundException(request.RunId);
            run = primaryDetail.Run;
        }

        ArchitectureAnalysisReport report = new()
        {
            Run = run
        };

        if (request.IncludeEvidence)
        {
            report.Evidence = await evidenceRepository.GetByRunIdAsync(request.RunId, cancellationToken);
            if (report.Evidence is null)

                report.Warnings.Add("Evidence package was not found for this run.");

        }

        if (request.IncludeExecutionTraces)
        {
            report.ExecutionTraces = (await traceRepository.GetByRunIdAsync(request.RunId, cancellationToken)).ToList();
            if (report.ExecutionTraces.Count == 0)

                report.Warnings.Add("No execution traces were found for this run.");

        }

        if (request.IncludeManifest)
        {
            report.Manifest = primaryDetail?.Manifest;

            string manifestVersionKey = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
                ? $"v1-{run.RunId}"
                : run.CurrentManifestVersion;

            if (report.Manifest is null)
            {
                report.Manifest = await unifiedGoldenManifestReader.GetByVersionAsync(manifestVersionKey, cancellationToken);

                if (report.Manifest is not null &&
                    !string.Equals(report.Manifest.RunId, run.RunId, StringComparison.Ordinal))

                    report.Manifest = null;

            }

            if (report.Manifest is null)

                report.Warnings.Add($"Manifest '{manifestVersionKey}' was not found.");

        }

        if (request.IncludeDiagram)

            if (report.Manifest is not null)

                report.Diagram = diagramGenerator.GenerateMermaid(report.Manifest);

            else

                report.Warnings.Add("Diagram was requested but the manifest is unavailable; diagram was not generated.");



        if (request.IncludeSummary)

            if (report.Manifest is not null)

                report.Summary = summaryGenerator.GenerateMarkdown(report.Manifest, report.Evidence);

            else

                report.Warnings.Add("Summary was requested but the manifest is unavailable; summary was not generated.");



        if (request.IncludeDeterminismCheck)

            report.Determinism = await determinismCheckService.RunAsync(
                new DeterminismCheckRequest
                {
                    RunId = request.RunId,
                    Iterations = request.DeterminismIterations,
                    ExecutionMode = ExecutionModeCurrent,
                    CommitReplays = false
                },
                cancellationToken);


        if (request.IncludeManifestCompare)

            if (string.IsNullOrWhiteSpace(request.CompareManifestVersion))

                report.Warnings.Add("Manifest comparison was requested but CompareManifestVersion was not provided.");

            else if (report.Manifest is null)

                report.Warnings.Add("Manifest comparison was requested but the primary manifest is not available.");

            else
            {
                GoldenManifest? compareManifest = await unifiedGoldenManifestReader.GetByVersionAsync(
                    request.CompareManifestVersion,
                    cancellationToken);

                if (compareManifest is null)

                    report.Warnings.Add($"Compare manifest '{request.CompareManifestVersion}' was not found.");

                else

                    report.ManifestDiff = manifestDiffService.Compare(report.Manifest, compareManifest);

            }


        if (!request.IncludeAgentResultCompare)
            return report;

        if (string.IsNullOrWhiteSpace(request.CompareRunId))

            report.Warnings.Add("Agent-result comparison was requested but CompareRunId was not provided.");

        else
        {
            ArchitectureRunDetail? compareDetail = await runDetailQueryService.GetRunDetailAsync(request.CompareRunId, cancellationToken);

            if (compareDetail is null)

                report.Warnings.Add($"Compare run '{request.CompareRunId}' was not found.");

            else
            {
                IReadOnlyList<AgentResult> leftResults = primaryDetail?.Results
                                                         ?? await resultRepository.GetByRunIdAsync(request.RunId, cancellationToken);
                List<AgentResult> rightResults = compareDetail.Results;

                report.AgentResultDiff = agentResultDiffService.Compare(
                    request.RunId,
                    leftResults,
                    request.CompareRunId,
                    rightResults);
            }
        }

        return report;
    }
}
