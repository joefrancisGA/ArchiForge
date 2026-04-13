using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentCompletionModelMetadataTests
{
    [Fact]
    public void TryConsume_returns_false_when_no_metadata_set()
    {
        AgentCompletionModelMetadata.TryConsume(out string? dep, out string? ver);

        dep.Should().BeNull();
        ver.Should().BeNull();
    }

    [Fact]
    public void TryConsume_returns_true_then_clears()
    {
        AzureOpenAiCompletionClient.TestingSetLastModelMetadata("my-deployment", "gpt-4o-mini");

        AgentCompletionModelMetadata.TryConsume(out string? dep, out string? ver);

        dep.Should().Be("my-deployment");
        ver.Should().Be("gpt-4o-mini");

        AgentCompletionModelMetadata.TryConsume(out dep, out ver);

        dep.Should().BeNull();
        ver.Should().BeNull();
    }
}
