namespace ArchLucid.Core.Resilience;

/// <summary>
///     Tuning for <see cref="CircuitBreakerGate" /> (Azure OpenAI completion / embedding protection).
/// </summary>
public sealed class CircuitBreakerOptions
{
    public const int DefaultFailureThreshold = 5;

    public const int DefaultDurationOfBreakSeconds = 30;

    /// <summary>Consecutive failures in the closed state before opening the circuit.</summary>
    public int FailureThreshold
    {
        get;
        set;
    } = DefaultFailureThreshold;

    /// <summary>How long the circuit stays open before a single recovery probe is allowed.</summary>
    public int DurationOfBreakSeconds
    {
        get;
        set;
    } = DefaultDurationOfBreakSeconds;

    /// <summary>Clamps invalid configuration to safe defaults.</summary>
    public void ApplyDefaults()
    {
        if (FailureThreshold < 1)
            FailureThreshold = DefaultFailureThreshold;

        if (DurationOfBreakSeconds < 1)
            DurationOfBreakSeconds = DefaultDurationOfBreakSeconds;
    }
}
