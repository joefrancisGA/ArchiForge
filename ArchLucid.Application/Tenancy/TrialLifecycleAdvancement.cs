namespace ArchLucid.Application.Tenancy;

/// <summary>One scheduler-driven trial lifecycle step.</summary>
public sealed class TrialLifecycleAdvancement
{
    public TrialLifecycleAdvancement(string fromStatus, string toStatus, string reason)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
    }

    public string FromStatus { get; }

    public string ToStatus { get; }

    public string Reason { get; }
}
