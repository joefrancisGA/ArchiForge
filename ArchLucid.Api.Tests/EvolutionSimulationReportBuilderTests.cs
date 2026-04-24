using System.Text.Json;

using ArchLucid.Api.Models.Evolution;
using ArchLucid.Api.Services.Evolution;
using ArchLucid.Contracts.Evolution;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
public sealed class EvolutionSimulationReportBuilderTests
{
    [Fact]
    public void Build_EmptyRuns_ProducesDocumentWithCandidateAndPlanAndDiffSectionReady()
    {
        string planJson = JsonSerializer.Serialize(
            new
            {
                planId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                themeId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                title = "Plan T",
                summary = "Plan S",
                priorityScore = 3,
                priorityExplanation = "Because tests.",
                status = "Open",
                actionStepCount = 2,
                linkedArchitectureRunIds = new[] { "run-1" }
            });

        EvolutionCandidateChangeSetRecord candidate = new()
        {
            CandidateChangeSetId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001"),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            SourcePlanId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Status = EvolutionCandidateChangeSetStatusValues.Draft,
            Title = "Candidate title",
            Summary = "Candidate summary body.",
            PlanSnapshotJson = planJson,
            DerivationRuleVersion = "60R-v1",
            CreatedUtc = DateTime.UtcNow
        };

        EvolutionSimulationReportDocument document =
            EvolutionSimulationReportBuilder.Build(candidate, [], DateTime.UtcNow);

        document.SchemaVersion.Should().Be(EvolutionSimulationReportDocument.ExportSchemaVersion);
        document.Candidate.Title.Should().Be("Candidate title");
        document.PlanSnapshot.Should().NotBeNull();
        document.PlanSnapshot!.Title.Should().Be("Plan T");
        document.SimulationRuns.Should().BeEmpty();

        string markdown = EvolutionSimulationReportMarkdownFormatter.Format(document);
        markdown.Should().Contain("Candidate summary body.");
        markdown.Should().Contain("Because tests.");
        markdown.Should().Contain("No simulation rows persisted");
    }

    [Fact]
    public void Build_WithEnvelopeOutcome_IncludesDiffSummaryAndEvaluationSectionInMarkdown()
    {
        string planJson = JsonSerializer.Serialize(
            new
            {
                planId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                themeId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                title = "Plan T",
                summary = "Plan S",
                priorityScore = 1,
                priorityExplanation = (string?)null,
                status = "Open",
                actionStepCount = 1,
                linkedArchitectureRunIds = new[] { "run-x" }
            });

        EvolutionCandidateChangeSetRecord candidate = new()
        {
            CandidateChangeSetId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000002"),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            SourcePlanId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Status = EvolutionCandidateChangeSetStatusValues.Simulated,
            Title = "Cand",
            Summary = "Cand sum",
            PlanSnapshotJson = planJson,
            DerivationRuleVersion = "60R-v1",
            CreatedUtc = DateTime.UtcNow
        };

        const string outcomeJson =
            """
            {"schemaVersion":"60R-v2","shadow":{"error":null,"architectureRunId":"run-x","evaluationMode":"ReadOnlyArchitectureAnalysis","runStatus":"Succeeded","manifestVersion":"1.0","hasManifest":true,"summaryLength":10,"warningCount":1},"evaluation":{"simulationScore":0.5,"determinismScore":null,"regressionRiskScore":null,"improvementDelta":0.1,"regressionSignals":["sig-a"],"confidenceScore":0.9},"explanationSummary":"All good."}
            """;

        EvolutionSimulationRunRecord run = new()
        {
            SimulationRunId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111"),
            CandidateChangeSetId = candidate.CandidateChangeSetId,
            BaselineArchitectureRunId = "run-x",
            EvaluationMode = EvolutionEvaluationModeValues.ReadOnlyArchitectureAnalysis,
            OutcomeJson = outcomeJson,
            WarningsJson = null,
            CompletedUtc = DateTime.UtcNow,
            IsShadowOnly = true
        };

        EvolutionSimulationReportDocument document =
            EvolutionSimulationReportBuilder.Build(candidate, [run], DateTime.UtcNow);

        document.SimulationRuns.Should().ContainSingle();
        document.SimulationRuns[0].DiffSummaryLines.Should().NotBeEmpty();
        document.SimulationRuns[0].DiffSummaryLines.Should().Contain(line =>
            line.Contains("appears on the plan snapshot", StringComparison.Ordinal));
        document.SimulationRuns[0].DiffSummaryLines.Should()
            .Contain(line => line.Contains("run status", StringComparison.OrdinalIgnoreCase));

        string markdown = EvolutionSimulationReportMarkdownFormatter.Format(document);
        markdown.Should().Contain("Diff summary");
        markdown.Should().Contain("Evaluation scores");
        markdown.Should().Contain("All good.");
    }
}
