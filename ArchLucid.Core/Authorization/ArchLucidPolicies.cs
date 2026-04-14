namespace ArchLucid.Core.Authorization;

/// <summary>
/// ASP.NET Core authorization policy names. Use these constants with <c>[Authorize(Policy = ...)]</c>
/// so registration and controllers stay aligned.
/// </summary>
public static class ArchLucidPolicies
{
    /// <summary>Required for read-only authority, manifest, governance query, and similar endpoints.</summary>
    public const string ReadAuthority = "ReadAuthority";

    /// <summary>Required for endpoints that create runs, replays, governance actions, and alert mutations.</summary>
    public const string ExecuteAuthority = "ExecuteAuthority";

    /// <summary>Required for host administration and policy-pack lifecycle (see RBAC table in docs/SECURITY.md).</summary>
    public const string AdminAuthority = "AdminAuthority";

    /// <summary>Same policy as <see cref="ReadAuthority"/> (alias for RBAC documentation).</summary>
    public const string RequireReadOnly = ReadAuthority;

    /// <summary>Same policy as <see cref="ExecuteAuthority"/>.</summary>
    public const string RequireOperator = ExecuteAuthority;

    /// <summary>Same policy as <see cref="AdminAuthority"/>.</summary>
    public const string RequireAdmin = AdminAuthority;

    /// <summary>Audit CSV/JSON export and other auditor-only surfaces.</summary>
    public const string RequireAuditor = "RequireAuditor";

    /// <summary>Internal replay diagnostics and execution traces.</summary>
    public const string CanViewReplayDiagnostics = "CanViewReplayDiagnostics";

    /// <summary>Comparison replay and persisting replay results.</summary>
    public const string CanReplayComparisons = "CanReplayComparisons";

    /// <summary>Merge agent results / golden manifest (<c>commit:run</c> permission).</summary>
    public const string CanCommitRuns = "CanCommitRuns";

    /// <summary>Consulting-template DOCX export (<c>export:consulting-docx</c> permission).</summary>
    public const string CanExportConsultingDocx = "CanExportConsultingDocx";
}
