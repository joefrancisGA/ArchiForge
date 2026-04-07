using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.AgentRuntime.Tests.Explanation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExplanationServiceRunTests
{
    private static GoldenManifest MinimalManifest() => new()
    {
        RunId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        ManifestId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        WorkspaceId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        ContextSnapshotId = Guid.NewGuid(),
        GraphSnapshotId = Guid.NewGuid(),
        FindingsSnapshotId = Guid.NewGuid(),
        DecisionTraceId = Guid.NewGuid(),
        CreatedUtc = DateTime.UtcNow,
        ManifestHash = "h",
        RuleSetId = "rs",
        RuleSetVersion = "1",
        RuleSetHash = "rsh",
        Metadata = new ManifestMetadata(),
        UnresolvedIssues = new UnresolvedIssuesSection(),
    };

    [Fact]
    public async Task ExplainRunAsync_valid_structured_json_populates_structured_and_raw_text()
    {
        const string llmJson =
            """
            {"schemaVersion":1,"reasoning":"Paragraph one.\n\nParagraph two.","evidenceRefs":["p1"],"confidence":0.9}
            """;

        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => llmJson);
        ExplanationService svc = new(client, NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().Be(llmJson);
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().Contain("Paragraph one");
        result.Structured.EvidenceRefs.Should().Equal("p1");
        result.Structured.Confidence.Should().Be(0.9m);
        result.DetailedNarrative.Should().Be(result.Structured.Reasoning);
        result.Summary.Should().Be("Paragraph one.");
    }

    [Fact]
    public async Task ExplainRunAsync_plain_text_wraps_structured_and_keeps_raw_text()
    {
        const string prose = "We chose the hub pattern because latency budgets require it.";

        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => prose);
        ExplanationService svc = new(client, NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().Be(prose);
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().Be(prose);
        result.DetailedNarrative.Should().Be(prose);
    }

    [Fact]
    public async Task ExplainRunAsync_legacy_summary_and_narrative_json_maps_to_structured_reasoning()
    {
        const string legacy = """{"summary":"Short","detailedNarrative":"Longer body here."}""";

        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => legacy);
        ExplanationService svc = new(client, NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().Be(legacy);
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().Be("Longer body here.");
        result.Summary.Should().Be("Short");
        result.DetailedNarrative.Should().Be("Longer body here.");
    }
}
