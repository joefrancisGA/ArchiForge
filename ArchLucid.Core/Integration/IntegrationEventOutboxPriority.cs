namespace ArchLucid.Core.Integration;

/// <summary>
///     Maps integration event logical types to <c>dbo.IntegrationEventOutbox.Priority</c> dequeue buckets:
///     0 = critical (operators / compliance), 1 = standard external, 2 = internal worker dispatch.
/// </summary>
public static class IntegrationEventOutboxPriority
{
    /// <returns>0 (critical), 1 (standard), or 2 (internal).</returns>
    public static int ForEventType(string? eventType)
    {
        if (eventType is null)
            return 1;

        string trimmed = eventType.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return 1;

        string canonical = IntegrationEventTypes.MapToCanonical(trimmed);

        return canonical.Length is 0 ? 1 : TierFromCanonical(canonical);
    }

    private static int TierFromCanonical(string canonical) => canonical switch
    {
        IntegrationEventTypes.AlertFiredV1 or IntegrationEventTypes.AlertResolvedV1
            or IntegrationEventTypes.ComplianceDriftEscalatedV1 => 0,
        IntegrationEventTypes.TrialLifecycleEmailV1 => 2,
        _ => 1,
    };
}
