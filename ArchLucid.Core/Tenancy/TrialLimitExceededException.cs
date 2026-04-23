namespace ArchLucid.Core.Tenancy;

/// <summary>
///     Thrown when a mutating operation is blocked because the tenant is on an active self-service trial that has
///     expired, exhausted included runs, or exceeded seat capacity.
/// </summary>
public sealed class TrialLimitExceededException : Exception
{
    public TrialLimitExceededException(TrialLimitReason reason, int daysRemaining, string? message = null)
        : base(message ?? BuildDefaultMessage(reason))
    {
        Reason = reason;
        DaysRemaining = daysRemaining;
    }

    public TrialLimitReason Reason
    {
        get;
    }

    /// <summary>Whole days remaining until <c>TrialExpiresUtc</c> when still active; <c>0</c> when expired or unknown.</summary>
    public int DaysRemaining
    {
        get;
    }

    private static string BuildDefaultMessage(TrialLimitReason reason)
    {
        return reason switch
        {
            TrialLimitReason.Expired => "The self-service trial has expired.",
            TrialLimitReason.RunsExceeded => "The trial included run allowance has been exhausted.",
            TrialLimitReason.SeatsExceeded => "The trial seat allowance has been exhausted.",
            TrialLimitReason.LifecycleWritesFrozen =>
                "This tenant is in a post-trial lifecycle phase where changes are not allowed.",
            TrialLimitReason.LifecycleDeletesFrozen =>
                "This tenant is in a read-only or export-only lifecycle phase where deletes are not allowed.",
            _ => "A trial limit was exceeded."
        };
    }
}
