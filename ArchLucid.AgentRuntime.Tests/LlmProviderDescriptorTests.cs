using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class LlmProviderDescriptorTests
{
    [Fact]
    public void ForAzureOpenAi_sets_kind_api_key_and_uri()
    {
        Uri uri = new("https://res.openai.azure.com/");

        LlmProviderDescriptor d = LlmProviderDescriptor.ForAzureOpenAi(uri, "my-deployment");

        d.ProviderKind.Should().Be("azure-openai");
        d.ModelId.Should().Be("my-deployment");
        d.ApiBaseUri.Should().Be(uri);
        d.AuthScheme.Should().Be(LlmProviderAuthScheme.ApiKey);
    }

    [Fact]
    public void ForAnthropic_and_ForBedrock_set_expected_auth()
    {
        Uri u = new("https://api.anthropic.com/");

        LlmProviderDescriptor a = LlmProviderDescriptor.ForAnthropic(u, "claude-3");
        a.AuthScheme.Should().Be(LlmProviderAuthScheme.ApiKey);
        a.ProviderKind.Should().Be("anthropic");

        LlmProviderDescriptor b =
            LlmProviderDescriptor.ForBedrock(new Uri("https://bedrock-runtime.us-east-1.amazonaws.com/"), "arn:model");
        b.AuthScheme.Should().Be(LlmProviderAuthScheme.AwsSigV4);
        b.ProviderKind.Should().Be("bedrock");
    }

    [Fact]
    public void AzureOpenAiCompletionClient_exposes_matching_descriptor()
    {
        AzureOpenAiCompletionClient sut = new(
            "https://unit-test.openai.azure.com/",
            "test-key",
            "gpt-test",
            AzureOpenAiCompletionClient.DefaultMaxCompletionTokens);

        sut.Descriptor.ProviderKind.Should().Be("azure-openai");
        sut.Descriptor.ModelId.Should().Be("gpt-test");
        sut.Descriptor.ApiBaseUri.Should().NotBeNull();
        sut.Descriptor.ApiBaseUri!.Host.Should().Contain("openai.azure.com");
        sut.Descriptor.AuthScheme.Should().Be(LlmProviderAuthScheme.ApiKey);
    }
}
