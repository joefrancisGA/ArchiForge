namespace ArchLucid.Api.Services.Admin;

/// <summary>
/// Result of <see cref="IAdminDiagnosticsService.RemediateOrphanGoldenManifestsAsync"/>.
/// </summary>
public sealed record OrphanGoldenManifestRemediationResult(
    bool DryRun,
    int RowCount,
    IReadOnlyList<string> ManifestIds);
