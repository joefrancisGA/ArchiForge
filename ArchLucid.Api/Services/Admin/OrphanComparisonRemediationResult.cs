namespace ArchLucid.Api.Services.Admin;

/// <summary>
/// Result of <see cref="IAdminDiagnosticsService.RemediateOrphanComparisonRecordsAsync"/>.
/// </summary>
public sealed record OrphanComparisonRemediationResult(
    bool DryRun,
    int RowCount,
    IReadOnlyList<string> ComparisonRecordIds);
