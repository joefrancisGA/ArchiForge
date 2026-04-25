namespace ArchLucid.Core.Resilience;

/// <summary>
///     Durable-audit payload for <see cref="CircuitBreakerGate" /> transitions (wired via optional callback).
/// </summary>
public sealed record CircuitBreakerAuditEntry(
    string GateName,
    string TransitionType,
    string FromState,
    string ToState,
    string? ProbeOutcome,
    DateTimeOffset OccurredUtc);
