namespace ArchLucid.Decisioning.Advisory.Learning;

/// <summary>
///     Per-dimension (category, urgency, signal type) counts and derived rates for learning profiles.
/// </summary>
public class RecommendationOutcomeStats
{
    /// <summary>Facet key (e.g. category name).</summary>
    public string Key
    {
        get;
        set;
    } = null!;

    /// <summary>Recommendations proposed in this bucket.</summary>
    public int ProposedCount
    {
        get;
        set;
    }

    public int AcceptedCount
    {
        get;
        set;
    }

    public int RejectedCount
    {
        get;
        set;
    }

    public int DeferredCount
    {
        get;
        set;
    }

    public int ImplementedCount
    {
        get;
        set;
    }

    /// <summary>Accepted ÷ proposed.</summary>
    public double AcceptanceRate =>
        ProposedCount == 0 ? 0 : (double)AcceptedCount / ProposedCount;

    /// <summary>Rejected ÷ proposed.</summary>
    public double RejectionRate =>
        ProposedCount == 0 ? 0 : (double)RejectedCount / ProposedCount;

    /// <summary>Deferred ÷ proposed.</summary>
    public double DeferredRate =>
        ProposedCount == 0 ? 0 : (double)DeferredCount / ProposedCount;

    /// <summary>Implemented ÷ proposed.</summary>
    public double ImplementationRate =>
        ProposedCount == 0 ? 0 : (double)ImplementedCount / ProposedCount;
}
