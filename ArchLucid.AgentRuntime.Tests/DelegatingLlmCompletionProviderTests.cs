using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class DelegatingLlmCompletionProviderTests
{
    [Fact]
    public void ctor_null_inner_throws()
    {
        Action act = () => _ = new DelegatingLlmCompletionProvider(null!, "p", "m");

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void ProviderId_whitespace_becomes_unknown()
    {
        FakeAgentCompletionClient inner = new((_, _) => "{}");

        DelegatingLlmCompletionProvider sut = new(inner, "   ", "dep");

        sut.ProviderId.Should().Be("unknown");
    }

    [Fact]
    public void ModelDeploymentLabel_whitespace_becomes_unknown()
    {
        FakeAgentCompletionClient inner = new((_, _) => "{}");

        DelegatingLlmCompletionProvider sut = new(inner, "pid", "  ");

        sut.ModelDeploymentLabel.Should().Be("unknown");
    }

    [Fact]
    public void Descriptor_layers_provider_kind_and_model_over_inner_descriptor()
    {
        LlmProviderDescriptor innerDesc = LlmProviderDescriptor.ForOffline("inner", "inner-model");
        FakeAgentCompletionClient inner = new((_, _) => "{}", innerDesc);
        DelegatingLlmCompletionProvider sut = new(inner, "layered", "deploy-x");

        sut.Descriptor.ProviderKind.Should().Be("layered");
        sut.Descriptor.ModelId.Should().Be("deploy-x");
        sut.Descriptor.ApiBaseUri.Should().Be(innerDesc.ApiBaseUri);
        sut.Descriptor.AuthScheme.Should().Be(innerDesc.AuthScheme);
    }

    [Fact]
    public async Task CompleteJsonAsync_delegates_to_inner()
    {
        FakeAgentCompletionClient inner = new((_, _) => """{"ok":true}""");
        DelegatingLlmCompletionProvider sut = new(inner, "p", "m");

        string json = await sut.CompleteJsonAsync("sys", "user", CancellationToken.None);

        json.Should().Be("""{"ok":true}""");
    }
}
