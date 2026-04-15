using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

/// <summary>
/// Keeps <c>tests/eval-datasets</c> aligned with <c>scripts/ci/eval_agent_quality.py</c> (shape-only until deterministic eval runs exist).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentEvalDatasetShapeTests
{
    private static string EvalDatasetsDirectory()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            string sln = Path.Combine(dir.FullName, "ArchLucid.sln");
            if (File.Exists(sln))
            {
                string root = Path.Combine(dir.FullName, "tests", "eval-datasets");
                if (Directory.Exists(root))
                {
                    return root;
                }
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not resolve tests/eval-datasets from test output directory.");
    }

    [Fact]
    public void Manifest_and_dataset_files_meet_minimum_shape()
    {
        string root = EvalDatasetsDirectory();
        string manifestPath = Path.Combine(root, "manifest.json");
        File.Exists(manifestPath).Should().BeTrue();

        JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement rootEl = manifest.RootElement;
        rootEl.GetProperty("schemaVersion").GetInt32().Should().Be(1);

        JsonElement datasets = rootEl.GetProperty("datasets");
        datasets.GetArrayLength().Should().BeGreaterThan(0);

        foreach (JsonElement entry in datasets.EnumerateArray())
        {
            string rel = entry.GetProperty("relativePath").GetString()!;
            int minCases = entry.GetProperty("minCases").GetInt32();
            string dataPath = Path.Combine(root, rel);
            File.Exists(dataPath).Should().BeTrue();

            JsonDocument data = JsonDocument.Parse(File.ReadAllText(dataPath));
            data.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            data.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(minCases);
        }
    }
}
