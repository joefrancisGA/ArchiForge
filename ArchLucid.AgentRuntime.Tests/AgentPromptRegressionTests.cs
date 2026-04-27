using System.Text.Json;
using System.Text.Json.Nodes;

using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.AgentSimulator.Services;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.GoldenCorpus;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Regresses agent system prompts and deterministic simulator <see cref="AgentResult" /> wire shapes against
///     <see cref="RealLlmOutputStructuralValidator" />. The simulator does not emit <c>findings[].trace</c> (not on the
///     finding DTO in contracts); the test harness **hydrates** the ExplainabilityTrace shape
///     before structural validation (same contract as the API) without changing simulator or handler code.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentPromptRegressionTests
{
    private const string BaselineFileName = "AgentPromptTemplateHashesBaseline.json";
    private const string RegressionRunId = "regression-prompt-structural-001";
    private static readonly IAgentSystemPromptCatalog PromptCatalog = AgentPromptCatalogTestFactory.Create();

    [Fact]
    public void Topology_system_prompt_hash_matches_baseline()
    {
        AssertPromptHash(AgentType.Topology, "topology");
    }

    [Fact]
    public void Compliance_system_prompt_hash_matches_baseline()
    {
        AssertPromptHash(AgentType.Compliance, "compliance");
    }

    [Fact]
    public void Critic_system_prompt_hash_matches_baseline()
    {
        AssertPromptHash(AgentType.Critic, "critic");
    }

    [Fact]
    public async Task Cost_agent_has_no_cataloged_system_prompt_simulator_wires_still_valid_under_contract()
    {
        IAgentHandler handler = new CostAgentHandler();

        Action act = () => PromptCatalog.Resolve(AgentType.Cost);
        act.Should()
            .Throw<InvalidOperationException>(
                "CostAgentHandler does not use IAgentSystemPromptCatalog — there is no built-in Cost template in CachedAgentSystemPromptCatalog.");

        AgentResult result = await handler.ExecuteAsync(
            RegressionRunId,
            BuildRequest(),
            BuildEvidence(RegressionRunId),
            new AgentTask
            {
                TaskId = "t-cost",
                RunId = RegressionRunId,
                AgentType = AgentType.Cost,
                Objective = "Cost."
            },
            CancellationToken.None);

        AssertStructuralValidationPasses(AgentType.Cost, result);
    }

    [Fact]
    public async Task Topology_deterministic_simulator_result_satisfies_structural_validator()
    {
        await RunSimulatorAndAssertStructureAsync(AgentType.Topology, "t-topo");
    }

    [Fact]
    public async Task Cost_deterministic_simulator_result_satisfies_structural_validator()
    {
        await RunSimulatorAndAssertStructureAsync(AgentType.Cost, "t-cost");
    }

    [Fact]
    public async Task Compliance_deterministic_simulator_result_satisfies_structural_validator()
    {
        await RunSimulatorAndAssertStructureAsync(AgentType.Compliance, "t-comp");
    }

    [Fact]
    public async Task Critic_deterministic_simulator_result_satisfies_structural_validator()
    {
        await RunSimulatorAndAssertStructureAsync(AgentType.Critic, "t-crit");
    }

    private static async Task RunSimulatorAndAssertStructureAsync(AgentType type, string taskId)
    {
        if (type != AgentType.Cost)
        {
            ResolvedSystemPrompt r = PromptCatalog.Resolve(type);
            r.Text.Should().NotBeNullOrWhiteSpace("same IAgentSystemPromptCatalog.Resolve path as LLM agent handlers");
        }

        DeterministicAgentSimulator sim = new();
        ArchitectureRequest request = BuildRequest();
        AgentEvidencePackage evidence = BuildEvidence(RegressionRunId);
        List<AgentTask> tasks =
        [
            new() { TaskId = taskId, RunId = RegressionRunId, AgentType = type, Objective = "Objective." }
        ];

        IReadOnlyList<AgentResult> list = await sim.ExecuteAsync(RegressionRunId, request, evidence, tasks);
        list.Should().HaveCount(1);
        AssertStructuralValidationPasses(type, list[0]);
    }

    private static void AssertStructuralValidationPasses(AgentType type, AgentResult result)
    {
        string wire = JsonSerializer.Serialize(result, ContractJson.Default);
        string forValidator = WithExplainabilityTracesHydratedForContract(wire);
        RealLlmStructuralValidationResult v =
            RealLlmOutputStructuralValidator.ValidateAgentResultStructure(type.ToString(), forValidator);

        v.IsValid.Should()
            .BeTrue(
                "simulator + trace hydration should satisfy RealLlmOutputStructuralValidator. Checks: {0}",
                string.Join(
                    "; ",
                    v.Checks.Select(static c => $"{c.Name}={(c.Passed ? "ok" : c.Message)}")));
    }

    /// <summary>
    ///     Inserts a minimal <c>trace</c> object on every finding, matching
    ///     <see cref="RealLlmOutputStructuralValidator" /> and production wire JSON, without mutating
    ///     <see cref="AgentResult" /> types.
    /// </summary>
    private static string WithExplainabilityTracesHydratedForContract(string agentResultJson)
    {
        JsonNode root = JsonNode.Parse(agentResultJson) ?? throw new InvalidOperationException("Result JSON is null.");
        if (root is not JsonObject obj)
            throw new InvalidOperationException("AgentResult JSON root must be an object.");

        if (obj["findings"] is not JsonArray findings)
            return agentResultJson;

        foreach (JsonNode? f in findings)
        {
            if (f is not JsonObject row)
                continue;

            if (row["trace"] is not null)
                continue;

            JsonObject trace = new()
            {
                ["sourceAgentExecutionTraceId"] = JsonValue.Create((string?)null),
                ["graphNodeIdsExamined"] = new JsonArray(),
                ["rulesApplied"] = new JsonArray(),
                ["decisionsTaken"] = new JsonArray(),
                ["alternativePathsConsidered"] = new JsonArray(),
                ["notes"] = new JsonArray()
            };
            row["trace"] = trace;
        }

        return root.ToJsonString(ContractJson.Default);
    }

    private static void AssertPromptHash(AgentType type, string baselineProperty)
    {
        string baselinePath = Path.Combine(AppContext.BaseDirectory, BaselineFileName);
        File.Exists(baselinePath).Should().BeTrue("missing {0} — add it next to the test class and set CopyToOutputDirectory.", baselinePath);

        string raw = File.ReadAllText(baselinePath);
        using JsonDocument doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty(baselineProperty, out JsonElement expectedEl))
            throw new InvalidOperationException($"Baseline JSON missing property '{baselineProperty}'.");

        string? expected = expectedEl.GetString();
        expected.Should().NotBeNullOrWhiteSpace("baseline for {0} must be a non-empty string.", baselineProperty);

        ResolvedSystemPrompt resolved = PromptCatalog.Resolve(type);
        string actual = resolved.ContentSha256Hex;
        string templateLabel = resolved.TemplateId + "@" + resolved.TemplateVersion;
        string failHint =
            $"System prompt text changed (template {templateLabel}). If intentional, update the '{baselineProperty}' entry in {BaselineFileName} to '{actual}' after a human review of prompt quality. Current SHA-256 (ContentSha256Hex) from CachedAgentSystemPromptCatalog: {actual}";

        actual.Should().Be(expected, failHint);
    }

    private static ArchitectureRequest BuildRequest()
    {
        return new ArchitectureRequest
        {
            RequestId = "req-regression-001",
            SystemName = "RagService",
            Description = "Regression test architecture request for agent prompt and simulator path.",
            Environment = "test",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["Use encryption in transit and at rest."]
        };
    }

    private static AgentEvidencePackage BuildEvidence(string runId)
    {
        return new AgentEvidencePackage
        {
            RunId = runId,
            RequestId = "req-regression-001",
            SystemName = "RagService",
            Environment = "test",
            CloudProvider = nameof(CloudProvider.Azure),
            Request = new RequestEvidence
            {
                Description = "Regression test system.",
                Constraints = ["encryption in transit and at rest"],
                RequiredCapabilities = [],
                Assumptions = []
            }
        };
    }

}
