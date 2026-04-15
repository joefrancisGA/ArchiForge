using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Composite;

namespace ArchLucid.Persistence;

/// <summary>
/// Default <see cref="IAlertSuppressionPolicy"/>: deduplicates composite fires using <see cref="IAlertRecordRepository.GetOpenByDeduplicationKeyAsync"/> and rule time windows.
/// </summary>
/// <param name="alertRepository">Looks up prior open/acknowledged alerts for the same dedupe key.</param>
/// <remarks>
/// Invoked from <see cref="CompositeAlertService"/> for each rule that evaluates to <c>true</c>.
/// </remarks>
public sealed class AlertSuppressionPolicy(IAlertRecordRepository alertRepository) : IAlertSuppressionPolicy
{
    /// <inheritdoc />
    public async Task<AlertSuppressionDecision> DecideAsync(
        CompositeAlertRule rule,
        AlertEvaluationContext context,
        AlertMetricSnapshot snapshot,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(snapshot);

        string dedupeKey = CompositeAlertDeduplicationKeyBuilder.Build(rule, context);

        AlertRecord? existing = await alertRepository
            .GetOpenByDeduplicationKeyAsync(
                context.TenantId,
                context.WorkspaceId,
                context.ProjectId,
                dedupeKey,
                ct)
            ;

        if (existing is null)

            return new AlertSuppressionDecision
            {
                ShouldCreateAlert = true,
                WasSuppressed = false,
                WasReopened = false,
                Reason = "No existing open or acknowledged alert matched the deduplication key.",
                DeduplicationKey = dedupeKey,
            };


        double ageMinutes = (DateTime.UtcNow - existing.CreatedUtc).TotalMinutes;

        if (ageMinutes < rule.CooldownMinutes)

            return new AlertSuppressionDecision
            {
                ShouldCreateAlert = false,
                WasSuppressed = true,
                WasReopened = false,
                Reason = $"Within cooldown window of {rule.CooldownMinutes} minutes since the prior alert.",
                DeduplicationKey = dedupeKey,
            };


        if (ageMinutes < rule.SuppressionWindowMinutes)

            return new AlertSuppressionDecision
            {
                ShouldCreateAlert = false,
                WasSuppressed = true,
                WasReopened = false,
                Reason = $"Within suppression window of {rule.SuppressionWindowMinutes} minutes.",
                DeduplicationKey = dedupeKey,
            };


        // Past windows but an open/acknowledged alert still exists — avoid duplicate rows with the same dedupe key.
        return new AlertSuppressionDecision
        {
            ShouldCreateAlert = false,
            WasSuppressed = true,
            WasReopened = false,
            Reason =
                "Suppression window elapsed, but a prior open or acknowledged alert still exists for this key. Resolve or acknowledge it to allow a new composite alert.",
            DeduplicationKey = dedupeKey,
        };
    }

}
