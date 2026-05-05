namespace ArchLucid.Cli.Commands;

/// <summary>Optional summary of a single <c>GET /v1/audit</c> page for the report.</summary>
internal sealed class ComplianceReportAuditLiveSample
{
    internal ComplianceReportAuditLiveSample(
        bool apiReached,
        string? errorNote,
        int eventsInPage,
        IReadOnlyDictionary<string, int> eventTypeCounts,
        DateTime? oldestUtc,
        DateTime? newestUtc)
    {
        ApiReached = apiReached;
        ErrorNote = errorNote;
        EventsInPage = eventsInPage;
        EventTypeCounts = eventTypeCounts;
        OldestUtc = oldestUtc;
        NewestUtc = newestUtc;
    }

    internal bool ApiReached
    {
        get;
    }

    internal string? ErrorNote
    {
        get;
    }

    internal int EventsInPage
    {
        get;
    }

    internal IReadOnlyDictionary<string, int> EventTypeCounts
    {
        get;
    }

    internal DateTime? OldestUtc
    {
        get;
    }

    internal DateTime? NewestUtc
    {
        get;
    }
}
