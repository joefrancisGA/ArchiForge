namespace ArchLucid.Application.Support;

/// <summary>Operator support-bundle retention hints (client-side deletion — bundles are not stored server-side).</summary>
public sealed class SupportBundleOptions
{
    public const string SectionPath = "ArchLucid:SupportBundle";

    /// <summary>Days after download when the ZIP should be purged from operator storage / ticketing systems.</summary>
    public int BundleRetentionDays
    {
        get;
        set;
    } = 30;
}
