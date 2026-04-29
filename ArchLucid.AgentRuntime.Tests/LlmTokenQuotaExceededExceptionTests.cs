using ArchLucid.Core;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class LlmTokenQuotaExceededExceptionTests
{
    [Fact]
    public void ctor_preserves_message()
    {
        LlmTokenQuotaExceededException ex = new("tenant quota exceeded for window");

        ex.Message.Should().Contain("tenant quota exceeded");
    }

    [Fact]
    public void ctor_sets_retry_after_when_provided()
    {
        DateTimeOffset retry = new(2026, 4, 30, 0, 0, 0, TimeSpan.Zero);
        LlmTokenQuotaExceededException ex = new("quota", retry);

        ex.RetryAfterUtc.Should().Be(retry);
    }

    [Fact]
    public void ctor_retry_after_defaults_to_null()
    {
        LlmTokenQuotaExceededException ex = new("quota");

        ex.RetryAfterUtc.Should().BeNull();
    }
}
