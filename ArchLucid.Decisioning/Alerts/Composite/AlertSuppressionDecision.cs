namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     Outcome of <see cref="IAlertSuppressionPolicy.DecideAsync" /> for a single composite rule match.
/// </summary>
public class AlertSuppressionDecision
{
    /// <summary>When <c>true</c>, <c>CompositeAlertService</c> should insert and deliver a new alert.</summary>
    public bool ShouldCreateAlert
    {
        get;
        set;
    }

    /// <summary>When <c>true</c>, the match was dropped due to cooldown, suppression window, or existing open row.</summary>
    public bool WasSuppressed
    {
        get;
        set;
    }

    /// <summary>Reserved for reopen flows; default policy leaves this <c>false</c>.</summary>
    public bool WasReopened
    {
        get;
        set;
    }

    /// <summary>Human-readable explanation for operators and audit payloads.</summary>
    public string Reason
    {
        get;
        set;
    } = null!;

    /// <summary>Key used for dedup lookup and stored on the created alert when applicable.</summary>
    public string DeduplicationKey
    {
        get;
        set;
    } = null!;
}
