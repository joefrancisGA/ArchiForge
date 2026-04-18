namespace ArchLucid.Core.Tenancy;

/// <summary>Values for <c>dbo.Tenants.TrialStatus</c> (self-service SaaS trial).</summary>
public static class TrialLifecycleStatus
{
    public const string Active = "Active";

    public const string Converted = "Converted";

    /// <summary>Writes blocked; reads allowed (see go-to-market trial policy).</summary>
    public const string Expired = "Expired";

    /// <summary>Writes and deletes blocked; export still allowed.</summary>
    public const string ReadOnly = "ReadOnly";

    /// <summary>No new export jobs; existing artifacts remain downloadable until purge.</summary>
    public const string ExportOnly = "ExportOnly";

    /// <summary>Terminal: row scheduled for removal after hard purge completes.</summary>
    public const string Deleted = "Deleted";
}
