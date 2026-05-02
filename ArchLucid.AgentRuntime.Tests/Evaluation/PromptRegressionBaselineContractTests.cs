using System.Text.Json;

using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

/// <summary>
///     Binds <c>scripts/ci/prompt_regression_baseline.json</c> (copied to output) to golden evaluator scores so CI fails
///     when prompt/handler changes regress committed floors. All agent types share the same <see cref="AgentResult" />
///     JSON contract;
///     the golden file uses <c>agentType: 1</c> (Topology) while evaluation passes each <see cref="AgentType" />
///     separately.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class PromptRegressionBaselineContractTests
{
    private const string TraceId = "trace-prompt-regression-baseline";

    private static readonly AgentOutputEvaluator Structural = new();
    private static readonly AgentOutputSemanticEvaluator Semantic = new();

    [SkippableFact]
    public void Golden_valid_fixture_meets_committed_prompt_regression_baseline_all_agent_types()
    {
        BaselineMins baseline = BaselineMins.LoadFromOutput();
        baseline.TopologyMinStructural.Should().BeGreaterThan(0.0);
        baseline.TopologyMinSemantic.Should().BeGreaterThan(0.0);

        AssertAgentMeetsFloor(
            LoadFixture("golden-agent-result-valid.json"),
            AgentType.Topology,
            baseline.TopologyMinStructural,
            baseline.TopologyMinSemantic);

        AssertAgentMeetsFloor(
            LoadFixture("golden-agent-result-cost.json"),
            AgentType.Cost,
            baseline.CostMinStructural,
            baseline.CostMinSemantic);

        AssertAgentMeetsFloor(
            LoadFixture("golden-agent-result-compliance.json"),
            AgentType.Compliance,
            baseline.ComplianceMinStructural,
            baseline.ComplianceMinSemantic);

        AssertAgentMeetsFloor(
            LoadFixture("golden-agent-result-critic.json"),
            AgentType.Critic,
            baseline.CriticMinStructural,
            baseline.CriticMinSemantic);
    }

    private static string LoadFixture(string fileName) =>
        File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "GoldenAgentResults", fileName));

    private static void AssertAgentMeetsFloor(string json, AgentType agentType, double minStructural,
        double minSemantic)
    {
        AgentOutputEvaluationScore structuralScore = Structural.Evaluate(TraceId, json, agentType);
        structuralScore.IsJsonParseFailure.Should().BeFalse();
        structuralScore.StructuralCompletenessRatio.Should().BeGreaterThanOrEqualTo(minStructural);

        AgentOutputSemanticScore semanticScore = Semantic.Evaluate(TraceId, json, agentType);
        semanticScore.OverallSemanticScore.Should().BeGreaterThanOrEqualTo(minSemantic);
    }

    private readonly struct BaselineMins
    {
        private BaselineMins(
            double topologyMinStructural,
            double topologyMinSemantic,
            double costMinStructural,
            double costMinSemantic,
            double complianceMinStructural,
            double complianceMinSemantic,
            double criticMinStructural,
            double criticMinSemantic)
        {
            TopologyMinStructural = topologyMinStructural;
            TopologyMinSemantic = topologyMinSemantic;
            CostMinStructural = costMinStructural;
            CostMinSemantic = costMinSemantic;
            ComplianceMinStructural = complianceMinStructural;
            ComplianceMinSemantic = complianceMinSemantic;
            CriticMinStructural = criticMinStructural;
            CriticMinSemantic = criticMinSemantic;
        }

        public double TopologyMinStructural
        {
            get;
        }

        public double TopologyMinSemantic
        {
            get;
        }

        public double CostMinStructural
        {
            get;
        }

        public double CostMinSemantic
        {
            get;
        }

        public double ComplianceMinStructural
        {
            get;
        }

        public double ComplianceMinSemantic
        {
            get;
        }

        public double CriticMinStructural
        {
            get;
        }

        public double CriticMinSemantic
        {
            get;
        }

        public static BaselineMins LoadFromOutput()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Regression",
                "prompt_regression_baseline.json");

            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Missing baseline copy at {path}; ensure ArchLucid.AgentRuntime.Tests links scripts/ci/prompt_regression_baseline.json.");
            }

            using FileStream stream = File.OpenRead(path);
            using JsonDocument doc = JsonDocument.Parse(stream);
            JsonElement root = doc.RootElement;
            JsonElement structBlock = root.GetProperty("minStructuralCompletenessByAgentType");
            JsonElement semBlock = root.GetProperty("minSemanticScoreByAgentType");

            return new BaselineMins(
                structBlock.GetProperty("Topology").GetDouble(),
                semBlock.GetProperty("Topology").GetDouble(),
                structBlock.GetProperty("Cost").GetDouble(),
                semBlock.GetProperty("Cost").GetDouble(),
                structBlock.GetProperty("Compliance").GetDouble(),
                semBlock.GetProperty("Compliance").GetDouble(),
                structBlock.GetProperty("Critic").GetDouble(),
                semBlock.GetProperty("Critic").GetDouble());
        }
    }
}
