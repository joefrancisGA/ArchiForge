using System.Text.Json;

using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.GoldenCohort;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.GoldenCohort;

/// <summary>
///     Reinforces the assumption behind <c>expectedCommittedManifestSha256</c> locking: the Simulator agent path must
///     return stable <see cref="AgentResult" /> payloads for a fixed <see cref="ArchitectureRequest" /> and task batch.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "GoldenCohort")]
public sealed class GoldenCohortSimulatorDeterminismTests
{
    private const int Repetitions = 10;

    private const string ProbeRunId = "golden-cohort-determinism-probe";

    [Fact]
    public async Task DeterministicAgentSimulator_produces_identical_results_for_each_cohort_item_over_ten_repetitions()
    {
        string cohortPath = Path.Combine(AppContext.BaseDirectory, "golden-cohort", "cohort.json");
        Assert.True(File.Exists(cohortPath),
            $"Missing {cohortPath} — link tests/golden-cohort/cohort.json in the test project.");

        GoldenCohortDocument document = GoldenCohortDocument.Load(cohortPath);
        DeterministicAgentSimulator simulator = new();

        foreach (GoldenCohortItem item in document.Items)
        {
            ArchitectureRequest request = GoldenCohortArchitectureRequestFactory.FromCohortItem(item);
            AgentEvidencePackage evidence = BuildEvidence(ProbeRunId, request);
            List<AgentTask> tasks = BuildStandardQuad(ProbeRunId);

            string? baselineJson = null;

            for (int i = 0; i < Repetitions; i++)
            {
                IReadOnlyList<AgentResult> results = await simulator.ExecuteAsync(ProbeRunId, request, evidence, tasks);
                string wire = JsonSerializer.Serialize(results, ContractJson.Default);

                baselineJson ??= wire;

                wire.Should().Be(baselineJson,
                    $"item {item.Id} repetition {i.ToString()} must match the Simulator baseline wire payload");
            }
        }
    }

    private static AgentEvidencePackage BuildEvidence(string runId, ArchitectureRequest request)
    {
        return new AgentEvidencePackage
        {
            RunId = runId,
            RequestId = request.RequestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
            Request = new RequestEvidence
            {
                Description = request.Description,
                Constraints = request.Constraints.ToList(),
                RequiredCapabilities = request.RequiredCapabilities.ToList(),
                Assumptions = request.Assumptions.ToList()
            }
        };
    }

    private static List<AgentTask> BuildStandardQuad(string runId)
    {
        return
        [
            new AgentTask
            {
                TaskId = "task-topology",
                RunId = runId,
                AgentType = AgentType.Topology,
                Objective = "Propose topology."
            },
            new AgentTask
            {
                TaskId = "task-cost", RunId = runId, AgentType = AgentType.Cost, Objective = "Estimate cost."
            },
            new AgentTask
            {
                TaskId = "task-compliance",
                RunId = runId,
                AgentType = AgentType.Compliance,
                Objective = "Check compliance."
            },
            new AgentTask
            {
                TaskId = "task-critic", RunId = runId, AgentType = AgentType.Critic, Objective = "Critique design."
            }
        ];
    }
}
