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
}
