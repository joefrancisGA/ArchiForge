using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

/// <summary>
///     Guards optional <c>qualityEvidence</c> blocks on <c>tests/eval-corpus</c> scenarios used by
///     <c>scripts/ci/eval_agent_corpus.py</c>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class EvalCorpusQualityEvidenceShapeTests
{
    private static string EvalCorpusDirectory()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null)
        {
            string sln = Path.Combine(dir.FullName, "ArchLucid.sln");
            if (File.Exists(sln))
            {
                string root = Path.Combine(dir.FullName, "tests", "eval-corpus");
                if (Directory.Exists(root))
                {
                    return root;
                }
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not resolve tests/eval-corpus from test output directory.");
    }

    [SkippableFact]
    public void Manifest_scenarios_with_quality_evidence_point_at_existing_simulator_agent_results()
    {
        string corpusDir = EvalCorpusDirectory();
        string manifestPath = Path.Combine(corpusDir, "manifest.json");
        File.Exists(manifestPath).Should().BeTrue();

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement scenarios = manifest.RootElement.GetProperty("scenarios");

        foreach (JsonElement rel in scenarios.EnumerateArray())
        {
            string? scenarioFile = rel.GetString();
            if (string.IsNullOrWhiteSpace(scenarioFile))
            {
                continue;
            }

            string scenarioPath = Path.Combine(corpusDir, scenarioFile);
            File.Exists(scenarioPath).Should().BeTrue($"missing scenario {scenarioFile}");

            using JsonDocument scen = JsonDocument.Parse(File.ReadAllText(scenarioPath));

            if (!scen.RootElement.TryGetProperty("qualityEvidence", out JsonElement qe))
            {
                continue;
            }

            qe.ValueKind.Should().Be(JsonValueKind.Object);
            string mode = qe.GetProperty("mode").GetString()!;
            mode.Should().NotBeNullOrWhiteSpace();

            if (string.Equals(mode, "simulator", StringComparison.OrdinalIgnoreCase))
            {
                string agentPath = qe.GetProperty("agentResultPath").GetString()!;
                agentPath.Should().NotBeNullOrWhiteSpace();
                File.Exists(Path.Combine(corpusDir, agentPath)).Should().BeTrue(
                    $"scenario {scenarioFile} missing {agentPath}");
            }
        }
    }
}
