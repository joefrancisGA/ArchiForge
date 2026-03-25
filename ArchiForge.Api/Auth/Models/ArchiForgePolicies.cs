namespace ArchiForge.Api.Auth.Models;

/// <summary>
/// Well-known ASP.NET Core authorization policy names registered by ArchiForge.
/// Use these constants wherever <c>[Authorize(Policy = ...)]</c> is applied to
/// prevent magic strings from drifting between policy registration and enforcement.
/// </summary>
public static class ArchiForgePolicies
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
}
