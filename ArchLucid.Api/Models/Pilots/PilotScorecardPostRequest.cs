namespace ArchLucid.Api.Models.Pilots;

/// <summary>Optional UTC window for <c>POST /v1/pilots/scorecard</c>; defaults to trailing 30 days when omitted.</summary>
public sealed class PilotScorecardPostRequest
{
    /// <summary>Inclusive range start (UTC).</summary>
    public DateTimeOffset? PeriodStart
    {
        get; set;
    }

    /// <summary>Exclusive range end (UTC).</summary>
    public DateTimeOffset? PeriodEnd
    {
        get; set;
    }
}
