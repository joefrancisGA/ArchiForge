using ArchLucid.Core.Resilience;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Resilience;

[Trait("Suite", "Core")]
public sealed class CircuitBreakerAuditEntryTests
{
    [Fact]
    public void Records_with_same_values_are_equal()
    {
        DateTimeOffset t = new(2026, 4, 8, 12, 0, 0, TimeSpan.Zero);
        CircuitBreakerAuditEntry a = new("g", "StateTransition", "Closed", "Open", null, t);
        CircuitBreakerAuditEntry b = new("g", "StateTransition", "Closed", "Open", null, t);

        a.Should().Be(b);
    }
}
