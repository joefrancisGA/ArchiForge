namespace ArchLucid.Application.Pilots;

/// <summary>
///     Builds a ZIP bundle of reference-evidence artifacts for a tenant (admin-only; uses ambient tenant scope).
/// </summary>
public interface IReferenceEvidenceAdminExportService
{
    /// <summary>
    ///     Returns ZIP bytes, or <see langword="null" /> when no suitable committed run exists.
    /// </summary>
    Task<byte[]?> BuildZipAsync(
        Guid tenantId,
        bool includeDemo,
        string apiBaseForLinks,
        CancellationToken cancellationToken = default);
}
