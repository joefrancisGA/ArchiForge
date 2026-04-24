using System.Text.Json;

using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.TestSupport.GoldenCorpus;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.GoldenCorpus;

/// <summary>Regenerates on-disk golden files (local only). Excluded from default CI filters.</summary>
[Trait("Category", "GoldenCorpusRecord")]
public sealed class GoldenCorpusMaterializerTests
{
    private const int CaseCount = 30;

    /// <summary>
    /// Set <c>ARCHLUCID_RECORD_DECISIONING_GOLDEN=1</c> to rewrite <c>tests/golden-corpus/decisioning/**</c> from current
    /// decisioning behavior (no LLM; frozen clock + simulator-only engines).
    /// </summary>
    [Fact]
    public async Task Record_all_cases_when_env_flag_set()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ARCHLUCID_RECORD_DECISIONING_GOLDEN"), "1", StringComparison.Ordinal))
            return;


        string compliance = Path.Combine(
            AppContext.BaseDirectory,
            "Compliance",
            "RulePacks",
            "default-compliance.rules.json");

        File.Exists(compliance).Should().BeTrue("compliance rule pack must be copied to test output.");

        FrozenUtcTimeProvider clock = new(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        GoldenCorpusHarness harness = new(compliance, clock);
        string root = GoldenCorpusRepoPaths.CorpusSourceDirectory;
        Directory.CreateDirectory(root);

        IReadOnlyList<GoldenCorpusCaseDefinition> cases = GoldenCorpusGraphFactory.BuildCases(CaseCount);

        foreach (GoldenCorpusCaseDefinition def in cases)
        {
            string dir = Path.Combine(root, def.CaseFolderName);
            Directory.CreateDirectory(dir);

            GoldenCorpusInputDocument input = new()
            {
                RunId = def.Graph.RunId,
                ContextSnapshotId = def.Graph.ContextSnapshotId,
                GraphSnapshot = def.Graph,
                Merge = def.Merge is null
                    ? null
                    : ToMergeDocument(def.Merge),
            };

            string inputJson = JsonSerializer.Serialize(input, GoldenCorpusJson.SerializerOptions);
            await File.WriteAllTextAsync(Path.Combine(dir, "input.json"), inputJson);

            CollectingAuditService audit = new();
            GoldenCorpusRunArtifacts artifacts = await harness.RunAsync(
                def.Graph.RunId,
                def.Graph.ContextSnapshotId,
                def.Graph,
                audit,
                def.Merge,
                CancellationToken.None);

            await File.WriteAllTextAsync(Path.Combine(dir, "expected-findings.json"), artifacts.FindingsJson);
            await File.WriteAllTextAsync(Path.Combine(dir, "expected-decisions.json"), artifacts.DecisionsJson);
            await File.WriteAllTextAsync(Path.Combine(dir, "expected-audit-types.json"), artifacts.AuditTypesJson);

            string readme =
                $"# {def.CaseFolderName}\n\n{def.ReadmeTitle}\n\nRegenerated with `ARCHLUCID_RECORD_DECISIONING_GOLDEN=1`.\n";
            await File.WriteAllTextAsync(Path.Combine(dir, "README.md"), readme);
        }
    }

    private static GoldenCorpusMergeDocument ToMergeDocument(GoldenCorpusMergeInput merge) => new()
    {
        MergeRunId = merge.MergeRunId,
        ManifestVersion = merge.ManifestVersion,
        Request = merge.Request,
        AgentResults = merge.AgentResults.ToList(),
        Evaluations = merge.Evaluations.ToList(),
        DecisionNodes = merge.DecisionNodes.ToList(),
        ParentManifestVersion = merge.ParentManifestVersion,
    };
}
