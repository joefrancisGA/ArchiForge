namespace ArchLucid.Api.Services.Admin;

/// <summary>
/// Result of <see cref="IAdminDiagnosticsService.RemediateOrphanFindingsSnapshotsAsync"/>.
/// </summary>
public sealed record OrphanFindingsSnapshotRemediationResult(
    bool DryRun,
    int RowCount,
    IReadOnlyList<string> FindingsSnapshotIds);
