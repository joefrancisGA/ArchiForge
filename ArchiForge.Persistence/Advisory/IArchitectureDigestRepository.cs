using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Persistence for <see cref="ArchitectureDigest"/> rows (<c>dbo.ArchitectureDigests</c> on SQL Server).
/// </summary>
/// <remarks>
/// Implementations: <see cref="DapperArchitectureDigestRepository"/>, <see cref="InMemoryArchitectureDigestRepository"/>.
/// Writers: <c>AdvisoryScanRunner</c> after digest build. Readers: <c>AdvisorySchedulingController</c> (list/get digests), <c>DigestSubscriptionsController</c> (delivery by digest id).
/// </remarks>
public interface IArchitectureDigestRepository
{
    /// <summary>Inserts a new digest row (no update/merge).</summary>
    Task CreateAsync(ArchitectureDigest digest, CancellationToken ct);

    /// <summary>Lists the most recently generated digests for the scope.</summary>
    /// <param name="workspaceId"></param>
    /// <param name="projectId"></param>
    /// <param name="take">Maximum rows (<c>TOP</c> on SQL Server).</param>
    /// <param name="tenantId"></param>
    /// <param name="ct"></param>
    /// <returns>Newest <see cref="ArchitectureDigest.GeneratedUtc"/> first.</returns>
    Task<IReadOnlyList<ArchitectureDigest>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct);

    /// <summary>Loads a digest by primary key (any scope — caller must enforce authorization).</summary>
    /// <returns>The digest, or <c>null</c> if missing.</returns>
    Task<ArchitectureDigest?> GetByIdAsync(Guid digestId, CancellationToken ct);
}
