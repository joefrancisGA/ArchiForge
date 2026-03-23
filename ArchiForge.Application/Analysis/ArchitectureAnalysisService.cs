using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Summaries;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

public sealed class ArchitectureAnalysisService(
    IArchitectureRunRepository runRepository,
    IGoldenManifestRepository manifestRepository,
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
    public async Task<ArchitectureAnalysisReport> BuildAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RunId))
        {
            throw new InvalidOperationException("RunId is required.");
        }

        var run = request.PreloadedRun
            ?? await runRepository.GetByIdAsync(request.RunId, cancellationToken)
            ?? throw new InvalidOperationException($"Run '{request.RunId}' not found.");

        var report = new ArchitectureAnalysisReport
        {
            Run = run
        };

        if (request.IncludeEvidence)
        {
            report.Evidence = await evidenceRepository.GetByRunIdAsync(request.RunId, cancellationToken);
            if (report.Evidence is null)
            {
                report.Warnings.Add("Evidence package was not found for this run.");
            }
        }

        if (request.IncludeExecutionTraces)
        {
            report.ExecutionTraces = (await traceRepository.GetByRunIdAsync(request.RunId, cancellationToken)).ToList();
            if (report.ExecutionTraces.Count == 0)
            {
                report.Warnings.Add("No execution traces were found for this run.");
            }
        }

        if (request.IncludeManifest && !string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
        {
            report.Manifest = await manifestRepository.GetByVersionAsync(run.CurrentManifestVersion!, cancellationToken);
            if (report.Manifest is null)
            {
                report.Warnings.Add($"Manifest '{run.CurrentManifestVersion}' was not found.");
            }
        }

        if (request.IncludeDiagram && report.Manifest is not null)
        {
            report.Diagram = diagramGenerator.GenerateMermaid(report.Manifest);
        }

        if (request.IncludeSummary && report.Manifest is not null)
        {
            report.Summary = summaryGenerator.GenerateMarkdown(report.Manifest, report.Evidence);
        }

        if (request.IncludeDeterminismCheck)
        {
            report.Determinism = await determinismCheckService.RunAsync(
                new DeterminismCheckRequest
                {
                    RunId = request.RunId,
                    Iterations = request.DeterminismIterations,
                    ExecutionMode = "Current",
                    CommitReplays = false
                },
                cancellationToken);
        }

        if (request.IncludeManifestCompare)
        {
            if (string.IsNullOrWhiteSpace(request.CompareManifestVersion))
            {
                report.Warnings.Add("Manifest comparison was requested but CompareManifestVersion was not provided.");
            }
            else if (report.Manifest is null)
            {
                report.Warnings.Add("Manifest comparison was requested but the primary manifest is not available.");
            }
            else
            {
                var compareManifest = await manifestRepository.GetByVersionAsync(
                    request.CompareManifestVersion,
                    cancellationToken);

                if (compareManifest is null)
                {
                    report.Warnings.Add($"Compare manifest '{request.CompareManifestVersion}' was not found.");
                }
                else
                {
                    report.ManifestDiff = manifestDiffService.Compare(report.Manifest, compareManifest);
                }
            }
        }

        if (!request.IncludeAgentResultCompare)
            return report;

        if (string.IsNullOrWhiteSpace(request.CompareRunId))
        {
            report.Warnings.Add("Agent-result comparison was requested but CompareRunId was not provided.");
        }
        else
        {
            var compareRun = await runRepository.GetByIdAsync(request.CompareRunId, cancellationToken);

            if (compareRun is null)
            {
                report.Warnings.Add($"Compare run '{request.CompareRunId}' was not found.");
            }
            else
            {
                var leftResults = await resultRepository.GetByRunIdAsync(request.RunId, cancellationToken);
                var rightResults = await resultRepository.GetByRunIdAsync(request.CompareRunId, cancellationToken);

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
