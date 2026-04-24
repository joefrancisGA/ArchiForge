using System.Text.Json;

using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.TestSupport.GoldenCorpus;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.GoldenCorpus;

/// <summary>Hard gate: golden corpus must match current in-process authority decisioning + optional merge slice.</summary>
[Trait("Suite", "Core")]
public sealed class GoldenCorpusRegressionTests
{
    private const int ExpectedCaseCount = 30;

    [Fact]
    public void Corpus_contains_thirty_case_directories()
    {
        string root = GoldenCorpusRepoPaths.CorpusOutputDirectory;
        Directory.Exists(root).Should().BeTrue($"missing corpus directory: {root}");

        int n = Directory.GetDirectories(root).Length;
        n.Should().Be(ExpectedCaseCount);
    }

    [Fact]
    public async Task All_cases_match_expected_outputs()
    {
        string compliance = Path.Combine(
            AppContext.BaseDirectory,
            "Compliance",
            "RulePacks",
            "default-compliance.rules.json");

        File.Exists(compliance).Should().BeTrue();

        FrozenUtcTimeProvider clock = new(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        GoldenCorpusHarness harness = new(compliance, clock);
        string root = GoldenCorpusRepoPaths.CorpusOutputDirectory;

        List<string> dirs = Directory.GetDirectories(root)
            .OrderBy(static d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

        dirs.Count.Should().Be(ExpectedCaseCount);

        foreach (string dir in dirs)
        {
            string inputPath = Path.Combine(dir, "input.json");
            File.Exists(inputPath).Should().BeTrue($"missing input.json in {dir}");

            string inputJson = await File.ReadAllTextAsync(inputPath);
            GoldenCorpusInputDocument? input =
                JsonSerializer.Deserialize<GoldenCorpusInputDocument>(inputJson, GoldenCorpusJson.SerializerOptions);

            input.Should().NotBeNull();
            GraphSnapshot graph = input!.GraphSnapshot;

            CollectingAuditService audit = new();
            GoldenCorpusMergeInput? merge = input.Merge?.ToModel();

            GoldenCorpusRunArtifacts actual = await harness.RunAsync(
                input.RunId,
                input.ContextSnapshotId,
                graph,
                audit,
                merge,
                CancellationToken.None);

            await AssertFileAsync(dir, "expected-findings.json", actual.FindingsJson);
            await AssertFileAsync(dir, "expected-decisions.json", actual.DecisionsJson);
            await AssertFileAsync(dir, "expected-audit-types.json", actual.AuditTypesJson);
        }
    }

    private static async Task AssertFileAsync(string caseDir, string fileName, string actualContent)
    {
        string expectedPath = Path.Combine(caseDir, fileName);
        File.Exists(expectedPath).Should().BeTrue($"missing {fileName} in {caseDir}");

        string expected = await File.ReadAllTextAsync(expectedPath);

        if (string.Equals(expected, actualContent, StringComparison.Ordinal))
            return;


        string dump = Path.Combine(caseDir, fileName + ".actual");
        await File.WriteAllTextAsync(dump, actualContent);

        actualContent.Should().Be(expected, $"golden mismatch; actual written to {dump}");
    }
}
