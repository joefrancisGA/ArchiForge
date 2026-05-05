namespace ArchLucid.Api.Models.Pilots;

public sealed class PilotCloseoutPostRequest
{
    public decimal? BaselineHours
    {
        get;
        set;
    }

    public int SpeedScore
    {
        get;
        set;
    }

    public int ManifestPackageScore
    {
        get;
        set;
    }

    public int TraceabilityScore
    {
        get;
        set;
    }

    public string? Notes
    {
        get;
        set;
    }

    /// <summary>Optional review run id (string form matches other pilot routes).</summary>
    public string? RunId
    {
        get;
        set;
    }
}
