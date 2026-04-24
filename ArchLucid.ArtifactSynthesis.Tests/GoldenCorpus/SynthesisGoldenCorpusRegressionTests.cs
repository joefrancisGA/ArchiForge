using System.Text.Json;

using ArchLucid.ArtifactSynthesis.Generators;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Decisioning.Models;
using ArchLucid.TestSupport.GoldenCorpus;

using FluentAssertions;

namespace ArchLucid.ArtifactSynthesis.Tests.GoldenCorpus;

/// <summary>Coverage summary JSON golden tests (no LLM; <see cref="CoverageSummaryArtifactGenerator" /> only).</summary>
[Trait("Suite", "Core")]
[Trait("Category", "GoldenCorpus")]
public sealed class SynthesisGoldenCorpusRegressionTests
{
    private static readonly JsonSerializerOptions JsonOptions = GoldenCorpusJson.SerializerOptions;

    private readonly CoverageSummaryArtifactGenerator _generator = new();

    [Theory]
    [InlineData("case-01")]
    [InlineData("case-02")]
    [InlineData("case-03")]
    public async Task Coverage_summary_case_matches_golden(string caseName)
    {
        string dir = Path.Combine(SynthesisGoldenCorpusPaths.CorpusOutputDirectory, caseName);
        string inputPath = Path.Combine(dir, "input.json");
        string expectedPath = Path.Combine(dir, "expected-output.json");
        File.Exists(inputPath).Should().BeTrue(inputPath);
        File.Exists(expectedPath).Should().BeTrue(expectedPath);

        string inputJson = await File.ReadAllTextAsync(inputPath);
        SynthesisGoldenInputDocument? doc =
            JsonSerializer.Deserialize<SynthesisGoldenInputDocument>(inputJson, JsonOptions);
        doc.Should().NotBeNull();

        GoldenManifest manifest = SynthesisGoldenManifestBuilder.Build(doc!);
        SynthesizedArtifact artifact = await _generator.GenerateAsync(manifest, CancellationToken.None);

        string expected = (await File.ReadAllTextAsync(expectedPath)).TrimEnd();
        artifact.Content.TrimEnd().Should().Be(expected, $"synthesis/{caseName} coverage summary JSON");
    }
}
