using ArchLucid.Core.Audit;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Audit;

[Trait("Suite", "Core")]
public sealed class AuditEventFilterTests
{
    [Fact]
    public void Default_Take_is_100()
    {
        AuditEventFilter filter = new();

        filter.Take.Should().Be(100);
    }
}
