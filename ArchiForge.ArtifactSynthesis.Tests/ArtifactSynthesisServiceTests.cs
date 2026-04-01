using FluentAssertions;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchiForge.ArtifactSynthesis.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArtifactSynthesisServiceTests
{
    [Fact]
    public async Task SynthesizeAsync_invokes_generators_in_artifact_type_order_and_validates()
    {
        List<string> order = [];
        Mock<IArtifactGenerator> genZ = new();
        genZ.Setup(x => x.ArtifactType).Returns("Zeta");
        genZ
            .Setup(x => x.GenerateAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()))
            .Returns(
                (GoldenManifest _, CancellationToken _) =>
                {
                    order.Add("Zeta");

                    return Task.FromResult(NewArtifact("Zeta", "z.txt", "z"));
                });

        Mock<IArtifactGenerator> genA = new();
        genA.Setup(x => x.ArtifactType).Returns("Alpha");
        genA
            .Setup(x => x.GenerateAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()))
            .Returns(
                (GoldenManifest _, CancellationToken _) =>
                {
                    order.Add("Alpha");

                    return Task.FromResult(NewArtifact("Alpha", "a.txt", "a"));
                });

        GoldenManifest manifest = NewManifest();
        ArtifactSynthesisService sut = new(
            [genZ.Object, genA.Object],
            new ArtifactBundleValidator(),
            NullLogger<ArtifactSynthesisService>.Instance);

        ArtifactBundle bundle = await sut.SynthesizeAsync(manifest, CancellationToken.None);

        order.Should().Equal("Alpha", "Zeta");
        bundle.Artifacts.Should().HaveCount(2);
        bundle.Trace.GeneratorsUsed.Should().Contain(x => x.Contains("ArtifactGenerator"));
    }

    [Fact]
    public async Task SynthesizeAsync_when_no_generators_adds_trace_note_and_validator_throws()
    {
        GoldenManifest manifest = NewManifest();
        ArtifactSynthesisService sut = new(
            Array.Empty<IArtifactGenerator>(),
            new ArtifactBundleValidator(),
            NullLogger<ArtifactSynthesisService>.Instance);

        Func<Task> act = async () => await sut.SynthesizeAsync(manifest, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*artifact*");
    }

    private static SynthesizedArtifact NewArtifact(string artifactType, string name, string content)
    {
        return new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = Guid.Empty,
            ManifestId = Guid.Empty,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = artifactType,
            Name = name,
            Format = "text",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content),
        };
    }

    private static GoldenManifest NewManifest()
    {
        Guid runId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid manifestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        return new GoldenManifest
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata { Name = "N" },
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Decisions =
            [
                new ResolvedArchitectureDecision
                {
                    DecisionId = "d1",
                    Category = "c",
                    Title = "t",
                    SelectedOption = "o",
                    Rationale = "r",
                },
            ],
        };
    }
}
