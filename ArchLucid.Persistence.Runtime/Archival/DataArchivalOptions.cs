namespace ArchLucid.Persistence.Archival;

/// <summary>
///     Retention-driven soft archival for authority runs, advisory digests, and Ask conversation threads.
/// </summary>
public sealed class DataArchivalOptions
{
    public const string SectionName = "DataArchival";

    /// <summary>When false, the hosted archival loop does nothing.</summary>
    public bool Enabled
    {
        get;
        set;
    }

    /// <summary>Archive runs with <c>CreatedUtc</c> older than this many days. 0 = skip runs.</summary>
    public int RunsRetentionDays
    {
        get;
        set;
    }

    /// <summary>Archive digests by <c>GeneratedUtc</c>. 0 = skip digests.</summary>
    public int DigestsRetentionDays
    {
        get;
        set;
    }

    /// <summary>Archive conversation threads by <c>LastUpdatedUtc</c>. 0 = skip threads.</summary>
    public int ConversationsRetentionDays
    {
        get;
        set;
    }

    /// <summary>Minimum wall-clock interval between archival passes.</summary>
    public int IntervalHours
    {
        get;
        set;
    } = 24;
}
