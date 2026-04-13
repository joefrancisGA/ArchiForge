using ArchLucid.AgentRuntime;
using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Validation;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

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

        IOptions<ExplanationServiceOptions> options = Options.Create(
            new ExplanationServiceOptions
            {
                AgentType = "unit-test-explanation",
                PromptTemplateId = "explain-run-json",
                PromptTemplateVersion = "v2026-04",
                PromptContentHash = "abc123",
            });
        IAgentCompletionClient client = new FakeAgentCompletionClient(
            (_, _) => llmJson,
            LlmProviderDescriptor.ForOffline("stub-llm", "model-under-test"));
        ExplanationService svc = new(
            client,
            new DeterministicExplanationService(NullLogger<DeterministicExplanationService>.Instance),
            options,
            new PassthroughSchemaValidationService(),
            NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().Be(llmJson);
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().Contain("Paragraph one");
        result.Structured.EvidenceRefs.Should().Equal("p1");
        result.Structured.Confidence.Should().Be(0.9m);
        result.Confidence.Should().Be(result.Structured.Confidence);
        result.Provenance.Should().NotBeNull();
        result.Provenance!.AgentType.Should().Be("unit-test-explanation");
        result.Provenance.ModelId.Should().Be("model-under-test");
        result.Provenance.PromptTemplateId.Should().Be("explain-run-json");
        result.Provenance.PromptTemplateVersion.Should().Be("v2026-04");
        result.Provenance.PromptContentHash.Should().Be("abc123");
        result.DetailedNarrative.Should().Be(result.Structured.Reasoning);
        result.Summary.Should().Be("Paragraph one.");
    }

    [Fact]
    public async Task ExplainRunAsync_plain_text_wraps_structured_and_keeps_raw_text()
    {
        const string prose = "We chose the hub pattern because latency budgets require it.";

        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => prose);
        ExplanationService svc = new(
            client,
            new DeterministicExplanationService(NullLogger<DeterministicExplanationService>.Instance),
            Options.Create(new ExplanationServiceOptions()),
            new PassthroughSchemaValidationService(),
            NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().Be(prose);
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().Be(prose);
        result.Confidence.Should().Be(result.Structured.Confidence);
        result.Provenance.Should().NotBeNull();
        result.DetailedNarrative.Should().Be(prose);
    }

    [Fact]
    public async Task ExplainRunAsync_legacy_summary_and_narrative_json_maps_to_structured_reasoning()
    {
        const string legacy = """{"summary":"Short","detailedNarrative":"Longer body here."}""";

        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => legacy);
        ExplanationService svc = new(
            client,
            new DeterministicExplanationService(NullLogger<DeterministicExplanationService>.Instance),
            Options.Create(new ExplanationServiceOptions()),
            new PassthroughSchemaValidationService(),
            NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().Be(legacy);
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().Be("Longer body here.");
        result.Confidence.Should().Be(result.Structured.Confidence);
        result.Provenance.Should().NotBeNull();
        result.Summary.Should().Be("Short");
        result.DetailedNarrative.Should().Be("Longer body here.");
    }

    [Fact]
    public async Task ExplainRunAsync_schemaVersion1_missing_reasoning_uses_deterministic_fallback()
    {
        const string invalidV1 = """{"schemaVersion":1}""";

        Mock<ILogger<SchemaValidationService>> schemaLog = new();
        SchemaValidationOptions schemaOpts = new()
        {
            AgentResultSchemaPath = "schemas/agentresult.schema.json",
            GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json",
            ExplanationRunSchemaPath = "schemas/explanation-run.schema.json",
            ComparisonExplanationSchemaPath = "schemas/comparison-explanation.schema.json",
        };
        SchemaValidationService schemaSvc = new(schemaLog.Object, Options.Create(schemaOpts));

        IAgentCompletionClient client = new FakeAgentCompletionClient((_, _) => invalidV1);
        ExplanationService svc = new(
            client,
            new DeterministicExplanationService(NullLogger<DeterministicExplanationService>.Instance),
            Options.Create(new ExplanationServiceOptions()),
            schemaSvc,
            NullLogger<ExplanationService>.Instance);

        ExplanationResult result = await svc.ExplainRunAsync(MinimalManifest(), null, CancellationToken.None);

        result.RawText.Should().BeEmpty();
        result.Structured.Should().NotBeNull();
        result.Structured!.Reasoning.Should().NotBeNullOrWhiteSpace();
        result.Structured.Reasoning.Should().NotBe(invalidV1);
    }
}
