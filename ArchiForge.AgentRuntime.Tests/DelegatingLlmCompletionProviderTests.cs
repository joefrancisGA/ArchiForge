using FluentAssertions;

namespace ArchiForge.AgentRuntime.Tests;

public sealed class DelegatingLlmCompletionProviderTests
{
    [Fact]
    public async Task Forwards_completion_and_exposes_labels()
    {
        IAgentCompletionClient inner = new FakeAgentCompletionClient((_, _) => """{"ok":true}""");
        ILlmCompletionProvider sut = new DelegatingLlmCompletionProvider(inner, "azure-openai", "gpt-deployment");

        sut.ProviderId.Should().Be("azure-openai");
        sut.ModelDeploymentLabel.Should().Be("gpt-deployment");

        string result = await sut.CompleteJsonAsync("sys", "user");

        result.Should().Contain("ok");
    }
}
