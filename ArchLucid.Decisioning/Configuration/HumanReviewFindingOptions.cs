namespace ArchLucid.Decisioning.Configuration;

/// <summary>Which persisted findings require human review before treated as authoritative.</summary>
public sealed class HumanReviewFindingOptions
{
    public const string SectionPath = "ArchLucid:Findings:HumanReview";

    /// <summary>
    ///     When non-empty, any finding whose <see cref="Models.Finding.FindingType" /> matches (ordinal-ignore) is marked
    ///     <see cref="FindingHumanReviewStatus.Pending" />.
    /// </summary>
    public List<string> RequiredFindingTypes
    {
        get;
        set;
    } = [];

    /// <summary>When true (default), non-deterministic findings at Critical/Error severity become Pending.</summary>
    public bool RequireForCriticalOrErrorWhenNotDeterministic
    {
        get;
        set;
    } = true;
}
