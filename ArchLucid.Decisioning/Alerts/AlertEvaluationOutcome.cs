namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Result of simple alert evaluation plus persistence.
/// </summary>
/// <param name="Evaluated">Every alert DTO produced by <see cref="IAlertEvaluator"/> this invocation (digest / reporting).</param>
/// <param name="NewlyPersisted">Subset actually inserted after deduplication against open/acknowledged keys.</param>
/// <remarks>
/// Returned from <c>ArchiForge.Persistence.Alerts.AlertService.EvaluateAndPersistAsync</c>.
/// </remarks>
public sealed record AlertEvaluationOutcome(
    IReadOnlyList<AlertRecord> Evaluated,
    IReadOnlyList<AlertRecord> NewlyPersisted);
