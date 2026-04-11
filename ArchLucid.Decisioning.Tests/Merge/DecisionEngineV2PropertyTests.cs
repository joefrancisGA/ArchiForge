using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Merge;

using FluentAssertions;

using FsCheck;

using FsCheck.Xunit;

namespace ArchLucid.Decisioning.Tests.Merge;

/// <summary>
/// Property-based checks for <see cref="DecisionEngineV2"/> with generated agent tasks, results, and evaluations.
/// </summary>
[Trait("Suite", "Core")]
public sealed class DecisionEngineV2PropertyTests
{
    private readonly DecisionEngineV2 _engine = new();

    [Fact]
    public async Task When_topology_task_only_without_result_returns_empty()
    {
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T-topo",
                RunId = "RUN-P",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
                Objective = "obj",
            },
        ];

        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-P",
            new ArchitectureRequest
            {
                RequestId = "R",
                SystemName = "SysNameHere",
                Description = "Description long enough.",
            },
            tasks,
            results: [],
            evaluations: []);

        decisions.Should().BeEmpty();
    }

#pragma warning disable xUnit1031 // FsCheck runs properties synchronously; DecisionEngineV2 exposes Task-only API.

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 80)]
    public void AlwaysProducesThreeDecisions_WhenTopologyPairExists(DecisionEngineTopologyInput input)
    {
        IReadOnlyList<DecisionNode> decisions = Resolve(input);

        decisions.Should().HaveCount(3);
        decisions.Select(d => d.Topic).Should().BeEquivalentTo(
            "TopologyAcceptance",
            "SecurityControlPromotion",
            "ComplexityDisposition");
    }

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 80)]
    public void EveryDecisionNodeHasNonEmptyRunId(DecisionEngineTopologyInput input)
    {
        IReadOnlyList<DecisionNode> decisions = Resolve(input);

        foreach (DecisionNode node in decisions)
        {
            node.RunId.Should().Be(input.RunId);
        }
    }

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 80)]
    public void EveryDecisionNodeHasASelectedOption(DecisionEngineTopologyInput input)
    {
        IReadOnlyList<DecisionNode> decisions = Resolve(input);

        foreach (DecisionNode node in decisions)
        {
            node.SelectedOptionId.Should().NotBeNullOrWhiteSpace();
            node.Options.Should().NotBeEmpty();
            node.Options.Select(o => o.OptionId).Should().Contain(node.SelectedOptionId);
        }
    }

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 80)]
    public void StructuralDeterminism_SameInputsSameTopicAndSelection(DecisionEngineTopologyInput input)
    {
        IReadOnlyList<DecisionNode> first = Resolve(input);
        IReadOnlyList<DecisionNode> second = Resolve(input);

        first.Should().HaveSameCount(second);

        foreach (DecisionNode a in first)
        {
            DecisionNode b = second.Single(x => x.Topic == a.Topic);
            string descriptionA = a.Options.Single(o => o.OptionId == a.SelectedOptionId).Description;
            string descriptionB = b.Options.Single(o => o.OptionId == b.SelectedOptionId).Description;
            descriptionB.Should().Be(descriptionA);
            b.Confidence.Should().Be(a.Confidence);
            b.Options.Should().HaveCount(a.Options.Count);
            b.Rationale.Should().Be(a.Rationale);
        }
    }

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 100)]
    public void NeverThrows_ForAnyValidInputCombination(DecisionEngineAnyValidInput input)
    {
        Action act = () => _engine.ResolveAsync(
                input.RunId,
                input.Request,
                input.Tasks,
                input.Results,
                input.Evaluations,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        act.Should().NotThrow();
    }

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 80)]
    public void OpposingEvaluationIds_AreSubsetOfInputEvaluations(DecisionEngineTopologyInput input)
    {
        HashSet<string> evaluationIds = input.Evaluations.Select(e => e.EvaluationId).ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<DecisionNode> decisions = Resolve(input);

        foreach (DecisionNode node in decisions)
        {
            foreach (string id in node.OpposingEvaluationIds)
            {
                evaluationIds.Should().Contain(id);
            }
        }
    }

    [Property(Arbitrary = [typeof(DecisionEngineArbitraries)], MaxTest = 80)]
    public void SupportingEvaluationIds_AreSubsetOfInputEvaluations(DecisionEngineTopologyInput input)
    {
        HashSet<string> evaluationIds = input.Evaluations.Select(e => e.EvaluationId).ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<DecisionNode> decisions = Resolve(input);

        foreach (DecisionNode node in decisions)
        {
            foreach (string id in node.SupportingEvaluationIds)
            {
                evaluationIds.Should().Contain(id);
            }
        }
    }

#pragma warning restore xUnit1031

    private IReadOnlyList<DecisionNode> Resolve(DecisionEngineTopologyInput input)
    {
        return _engine.ResolveAsync(
                input.RunId,
                input.Request,
                input.Tasks,
                input.Results,
                input.Evaluations,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
}

/// <summary>FsCheck generators for <see cref="DecisionEngineV2PropertyTests"/>.</summary>
public static class DecisionEngineArbitraries
{
    public static Arbitrary<DecisionEngineTopologyInput> DecisionEngineTopologyInputs()
    {
        return TopologyPairGen().ToArbitrary();
    }

    public static Arbitrary<DecisionEngineAnyValidInput> DecisionEngineAnyValidInputs()
    {
        Gen<DecisionEngineAnyValidInput> gen = Gen.Frequency(
            Tuple.Create(5, TopologyPairGen().Select(t => new DecisionEngineAnyValidInput(t.RunId, t.Request, t.Tasks, t.Results, t.Evaluations))),
            Tuple.Create(1, TopologyTaskOnlyGen()),
            Tuple.Create(1, NoTopologyGen()));

        return gen.ToArbitrary();
    }

    private static Gen<DecisionEngineTopologyInput> TopologyPairGen()
    {
        return from seedBundle in SeedBundleGen()
               select DecisionEngineInputBuilder.BuildTopologyPair(seedBundle);
    }

    private static Gen<DecisionEngineAnyValidInput> TopologyTaskOnlyGen()
    {
        return from s1 in Arb.Default.Int32().Generator
               from s2 in Arb.Default.Int32().Generator
               from s3 in Arb.Default.Int32().Generator
               from s4 in Arb.Default.Int32().Generator
               from s5 in Arb.Default.Int32().Generator
               from s6 in Arb.Default.Int32().Generator
               from s7 in Arb.Default.Int32().Generator
               select DecisionEngineInputBuilder.BuildTopologyTaskOnly(s1, s2, s3, s4, s5, s6, s7);
    }

    private static Gen<DecisionEngineAnyValidInput> NoTopologyGen()
    {
        return from s1 in Arb.Default.Int32().Generator
               from s2 in Arb.Default.Int32().Generator
               from s3 in Arb.Default.Int32().Generator
               from s4 in Arb.Default.Int32().Generator
               select DecisionEngineInputBuilder.BuildNoTopology(s1, s2, s3, s4);
    }

    private static Gen<(int, int, int, int, int, int, int, int, int)> SeedBundleGen()
    {
        return from a in Arb.Default.Int32().Generator
               from b in Arb.Default.Int32().Generator
               from c in Arb.Default.Int32().Generator
               from d in Arb.Default.Int32().Generator
               from e in Arb.Default.Int32().Generator
               from f in Arb.Default.Int32().Generator
               from g in Arb.Default.Int32().Generator
               from h in Arb.Default.Int32().Generator
               from i in Arb.Default.Int32().Generator
               select (a, b, c, d, e, f, g, h, i);
    }
}

/// <summary>Inputs that always include a topology task and a topology result (engine emits three decisions).</summary>
public sealed record DecisionEngineTopologyInput(
    string RunId,
    ArchitectureRequest Request,
    IReadOnlyList<AgentTask> Tasks,
    IReadOnlyList<AgentResult> Results,
    IReadOnlyList<AgentEvaluation> Evaluations);

/// <summary>Valid inputs that may omit a topology pair (still non-null collections and non-blank run id).</summary>
public sealed record DecisionEngineAnyValidInput(
    string RunId,
    ArchitectureRequest Request,
    IReadOnlyList<AgentTask> Tasks,
    IReadOnlyList<AgentResult> Results,
    IReadOnlyList<AgentEvaluation> Evaluations);

internal static class DecisionEngineInputBuilder
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    internal static DecisionEngineTopologyInput BuildTopologyPair((int a, int b, int c, int d, int e, int f, int g, int h, int i) seeds)
    {
        string runId = AlphaNumFromSeed(seeds.a, 8, 24);
        string reqId = AlphaNumFromSeed(seeds.b, 6, 16);
        string sysName = AlphaNumFromSeed(seeds.c, 4, 20);
        string desc = EnsureMinDescriptionLength(AlphaNumFromSeed(seeds.d, 10, 40));
        string topoTaskId = AlphaNumFromSeed(seeds.e, 6, 16);
        string mismatchedResultTaskId = AlphaNumFromSeed(seeds.f, 6, 16);
        int extraCount = Math.Abs(seeds.g) % 3;
        int evalCount = Math.Abs(seeds.h) % 7;
        int rationaleMode = Math.Abs(seeds.i) % 5;

        ArchitectureRequest request = new()
        {
            RequestId = reqId,
            SystemName = sysName,
            Description = desc,
        };

        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = topoTaskId,
                RunId = runId,
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
                Objective = "topology objective",
            },
        ];

        List<string> taskIds = [topoTaskId];
        AgentType[] extraTypes = [AgentType.Compliance, AgentType.Critic, AgentType.Cost];

        for (int i = 0; i < extraCount; i++)
        {
            string tid = AlphaNumFromSeed(seeds.g + i * 7919, 6, 14);
            if (taskIds.Contains(tid, StringComparer.Ordinal))
            {
                tid = tid + "x" + i;
            }

            taskIds.Add(tid);
            tasks.Add(
                new AgentTask
                {
                    TaskId = tid,
                    RunId = runId,
                    AgentType = extraTypes[i % extraTypes.Length],
                    Status = AgentTaskStatus.Completed,
                    Objective = "extra objective",
                });
        }

        double topoConfidence = (Math.Abs(seeds.a ^ seeds.b) % 1001) / 1000.0;

        List<AgentResult> results =
        [
            new()
            {
                RunId = runId,
                TaskId = mismatchedResultTaskId,
                AgentType = AgentType.Topology,
                Confidence = topoConfidence,
                ResultId = AlphaNumFromSeed(seeds.e ^ seeds.f, 8, 16),
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        List<AgentEvaluation> evaluations = BuildEvaluations(runId, taskIds, evalCount, rationaleMode, seeds.h);

        return new DecisionEngineTopologyInput(runId, request, tasks, results, evaluations);
    }

    internal static DecisionEngineAnyValidInput BuildTopologyTaskOnly(int s1, int s2, int s3, int s4, int s5, int s6, int s7)
    {
        string runId = AlphaNumFromSeed(s1, 8, 20);
        ArchitectureRequest request = new()
        {
            RequestId = AlphaNumFromSeed(s2, 6, 14),
            SystemName = AlphaNumFromSeed(s3, 4, 18),
            Description = EnsureMinDescriptionLength(AlphaNumFromSeed(s4, 10, 36)),
        };

        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = AlphaNumFromSeed(s5, 6, 14),
                RunId = runId,
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
                Objective = "topology objective",
            },
        ];

        List<AgentEvaluation> evaluations = BuildEvaluations(
            runId,
            [tasks[0].TaskId],
            Math.Abs(s6) % 4,
            Math.Abs(s7) % 5,
            s7);

        return new DecisionEngineAnyValidInput(runId, request, tasks, [], evaluations);
    }

    internal static DecisionEngineAnyValidInput BuildNoTopology(int s1, int s2, int s3, int s4)
    {
        string runId = AlphaNumFromSeed(s1, 8, 20);
        ArchitectureRequest request = new()
        {
            RequestId = AlphaNumFromSeed(s2, 6, 14),
            SystemName = AlphaNumFromSeed(s3, 4, 18),
            Description = EnsureMinDescriptionLength(AlphaNumFromSeed(s4, 10, 36)),
        };

        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = AlphaNumFromSeed(s1 ^ s2, 6, 14),
                RunId = runId,
                AgentType = AgentType.Compliance,
                Status = AgentTaskStatus.Completed,
                Objective = "compliance objective",
            },
        ];

        return new DecisionEngineAnyValidInput(runId, request, tasks, [], []);
    }

    private static List<AgentEvaluation> BuildEvaluations(
        string runId,
        List<string> taskIds,
        int evalCount,
        int rationaleMode,
        int seed)
    {
        string[] types =
        [
            EvaluationTypes.Support,
            EvaluationTypes.Strengthen,
            EvaluationTypes.Oppose,
            EvaluationTypes.Caution,
        ];

        List<AgentEvaluation> list = [];

        for (int i = 0; i < evalCount; i++)
        {
            int s = seed + i * 486187739;
            string taskId = taskIds[Math.Abs(s) % taskIds.Count];
            string evalType = types[Math.Abs(s >> 3) % types.Length];
            double delta = (Math.Abs(s >> 5) % 201 - 100) / 100.0;
            string rationale = RationaleForMode((rationaleMode + i) % 5, s);

            list.Add(
                new AgentEvaluation
                {
                    EvaluationId = AlphaNumFromSeed(s ^ 0x5F3759DF, 10, 20),
                    RunId = runId,
                    TargetAgentTaskId = taskId,
                    EvaluationType = evalType,
                    ConfidenceDelta = delta,
                    Rationale = rationale,
                });
        }

        return list;
    }

    private static string RationaleForMode(int mode, int seed)
    {
        return mode switch
        {
            0 => "private endpoints matter " + AlphaNumFromSeed(seed, 4, 12),
            1 => "managed identity is preferred " + AlphaNumFromSeed(seed, 4, 12),
            2 => "general rationale " + AlphaNumFromSeed(seed, 6, 20),
            3 => "support signal " + AlphaNumFromSeed(seed, 4, 16),
            _ => "evaluation note " + AlphaNumFromSeed(seed, 5, 18),
        };
    }

    private static string AlphaNumFromSeed(int seed, int minLen, int maxLen)
    {
        unchecked
        {
            int span = maxLen - minLen + 1;
            int len = span > 0 ? minLen + Math.Abs(seed) % span : minLen;
            char[] buf = new char[len];

            for (int i = 0; i < len; i++)
            {
                seed = seed * 1103515245 + 12345;
                buf[i] = Alphabet[Math.Abs(seed) % Alphabet.Length];
            }

            return new string(buf);
        }
    }

    private static string EnsureMinDescriptionLength(string description)
    {
        const string pad = "0123456789abcdef";

        if (description.Length >= 10)
        {
            return description;
        }

        return description + pad[..(10 - description.Length)];
    }
}
