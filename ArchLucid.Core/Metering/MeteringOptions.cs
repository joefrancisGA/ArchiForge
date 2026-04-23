namespace ArchLucid.Core.Metering;

/// <summary>Feature flags for usage metering persistence.</summary>
public sealed class MeteringOptions
{
    public const string SectionName = "Metering";

    /// <summary>When false, <see cref="IUsageMeteringService" /> no-ops (except summaries return empty).</summary>
    public bool Enabled
    {
        get;
        set;
    }
}
