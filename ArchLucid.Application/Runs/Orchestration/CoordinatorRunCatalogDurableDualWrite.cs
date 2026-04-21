using ArchLucid.Core.Audit;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
/// ADR 0021 Phase 2: append two durable <c>dbo.AuditEvents</c> rows per coordinator-stage signal — legacy <c>CoordinatorRun*</c>
/// plus canonical <see cref="AuditEventTypes.Run"/> — until dashboards migrate and legacy constants sunset.
/// </summary>
internal static class CoordinatorRunCatalogDurableDualWrite
{
    /// <summary>
    /// Persists <paramref name="legacyEvent"/> then a copy whose <see cref="AuditEvent.EventType"/> is <paramref name="canonicalEventType"/>.
    /// </summary>
    public static async Task LogTwiceAsync(
        IAuditService auditService,
        AuditEvent legacyEvent,
        string canonicalEventType,
        CancellationToken cancellationToken)
    {
        if (auditService is null)
            throw new ArgumentNullException(nameof(auditService));

        if (legacyEvent is null)
            throw new ArgumentNullException(nameof(legacyEvent));

        if (string.IsNullOrWhiteSpace(canonicalEventType))
            throw new ArgumentException("Canonical event type is required.", nameof(canonicalEventType));

        await auditService.LogAsync(legacyEvent, cancellationToken).ConfigureAwait(false);

        AuditEvent canonical = CopyForCanonicalRow(legacyEvent, canonicalEventType);
        await auditService.LogAsync(canonical, cancellationToken).ConfigureAwait(false);
    }

    private static AuditEvent CopyForCanonicalRow(AuditEvent source, string canonicalEventType) => new()
    {
        EventType = canonicalEventType,
        ActorUserId = source.ActorUserId,
        ActorUserName = source.ActorUserName,
        TenantId = source.TenantId,
        WorkspaceId = source.WorkspaceId,
        ProjectId = source.ProjectId,
        RunId = source.RunId,
        ManifestId = source.ManifestId,
        ArtifactId = source.ArtifactId,
        DataJson = source.DataJson,
        CorrelationId = source.CorrelationId,
        OccurredUtc = source.OccurredUtc,
    };
}
