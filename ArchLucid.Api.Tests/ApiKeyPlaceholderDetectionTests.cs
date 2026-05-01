using ArchLucid.Host.Core.Startup.Validation.Rules;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiKeyPlaceholderDetectionTests
{
    [SkippableFact]
    public void IsPlaceholderValue_WhenNullOrEmptyOrWhitespace_returns_false()
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue(null).Should().BeFalse();
        ApiKeyPlaceholderDetection.IsPlaceholderValue(string.Empty).Should().BeFalse();
        ApiKeyPlaceholderDetection.IsPlaceholderValue("   ").Should().BeFalse();
    }

    [Theory]
    [InlineData("changeme")]
    [InlineData("CHANGEME")]
    [InlineData("placeholder")]
    [InlineData("test")]
    [InlineData("example")]
    [InlineData("default")]
    [InlineData("demo")]
    [InlineData("key")]
    [InlineData("apikey")]
    [InlineData("api-key")]
    public void IsPlaceholderValue_WhenExactBlocklist_returns_true(string value)
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue(value).Should().BeTrue();
        ApiKeyPlaceholderDetection.IsPlaceholderValue($"  {value}  ").Should().BeTrue();
    }

    [SkippableFact]
    public void IsPlaceholderValue_WhenSubstringTodo_returns_true()
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue("my-todo-key").Should().BeTrue();
    }

    [SkippableFact]
    public void IsPlaceholderValue_WhenShort_returns_true()
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue("short").Should().BeTrue();
    }

    [SkippableFact]
    public void IsPlaceholderValue_WhenNineteenCharacters_returns_true()
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue("nineteencharacters!").Should().BeTrue();
    }

    [SkippableFact]
    public void IsPlaceholderValue_WhenTwentyCharRandom_returns_false()
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue("aB3$xK9mN2pQ7wR5vZ1y").Should().BeFalse();
    }

    [SkippableFact]
    public void IsPlaceholderValue_WhenLongRandom_returns_false()
    {
        ApiKeyPlaceholderDetection.IsPlaceholderValue("a-]K9mN2pQ7wR5vZ1yLongEnoughAndRandom").Should().BeFalse();
    }
}
