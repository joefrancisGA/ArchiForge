using ArchLucid.AgentRuntime.Safety;
using ArchLucid.Core.Safety;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class NullContentSafetyGuardTests
{
    [Fact]
    public async Task CheckInputAsync_returns_allowed_without_categories()
    {
        NullContentSafetyGuard sut = new();

        ContentSafetyResult result = await sut.CheckInputAsync("any text", CancellationToken.None);

        result.IsAllowed.Should().BeTrue();
        result.Category.Should().BeNull();
    }

    [Fact]
    public async Task CheckOutputAsync_returns_allowed_without_categories()
    {
        NullContentSafetyGuard sut = new();

        ContentSafetyResult result = await sut.CheckOutputAsync("any text", CancellationToken.None);

        result.IsAllowed.Should().BeTrue();
        result.Category.Should().BeNull();
    }
}
