namespace ArchLucid.Api.Auth.Models;

/// <summary>
/// Well-known ASP.NET Core authorization policy names registered by ArchLucid.
/// Use these constants wherever <c>[Authorize(Policy = ...)]</c> is applied to
/// prevent magic strings from drifting between policy registration and enforcement.
/// </summary>
public static class ArchLucidPolicies
{
    /// <summary>Required for all read-only authority, manifest, and governance query endpoints.</summary>
    public const string ReadAuthority = "ReadAuthority";

    /// <summary>Required for endpoints that trigger runs, replays, or governance promotions.</summary>
    public const string ExecuteAuthority = "ExecuteAuthority";

    /// <summary>Required for administrative operations (policy management, user administration).</summary>
    public const string AdminAuthority = "AdminAuthority";

    /// <summary>Required for viewing internal replay diagnostics and execution traces.</summary>
    public const string CanViewReplayDiagnostics = "CanViewReplayDiagnostics";

    /// <summary>Required for triggering comparison replay and persisting replay results.</summary>
    public const string CanReplayComparisons = "CanReplayComparisons";

    /// <summary>Required to merge agent results and persist a golden manifest (<c>commit:run</c> permission).</summary>
    public const string CanCommitRuns = "CanCommitRuns";

    /// <summary>Required to export consulting-template DOCX analysis reports (<c>export:consulting-docx</c> permission).</summary>
    public const string CanExportConsultingDocx = "CanExportConsultingDocx";
}
