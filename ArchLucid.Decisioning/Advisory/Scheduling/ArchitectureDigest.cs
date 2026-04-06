namespace ArchiForge.Decisioning.Advisory.Scheduling;

/// <summary>
/// Persisted “daily” (or scheduled) architecture summary for a scope: markdown body, short summary, optional run linkage, and opaque <see cref="MetadataJson"/> for counts and diagnostics.
/// </summary>
/// <remarks>
/// Produced by <see cref="IArchitectureDigestBuilder"/> inside <c>AdvisoryScanRunner</c>, stored via <c>IArchitectureDigestRepository</c>, and optionally delivered by <see cref="ArchiForge.Decisioning.Advisory.Delivery.IDigestDeliveryDispatcher"/>.
/// HTTP list/get: <c>ArchiForge.Api.Controllers.AdvisorySchedulingController</c>.
/// </remarks>
public class ArchitectureDigest
{
    public Guid DigestId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string ContentMarkdown { get; set; } = null!;
    public string MetadataJson { get; set; } = "{}";

    /// <summary>When set, digest list/get APIs treat the row as archived (soft delete from operator views).</summary>
    public DateTime? ArchivedUtc { get; set; }
}
