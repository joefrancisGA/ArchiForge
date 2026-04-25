namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     Result of composite alert evaluation: rows created after suppression, plus count of rule matches that were
///     suppressed.
/// </summary>
/// <param name="Created">Newly persisted composite alerts (one per accepted match).</param>
/// <param name="SuppressedMatchCount">
///     Rules that evaluated <c>true</c> but <see cref="IAlertSuppressionPolicy" /> declined
///     creation.
/// </param>
/// <remarks>
///     Returned from <c>ArchLucid.Persistence.Alerts.CompositeAlertService.EvaluateAndPersistAsync</c>.
/// </remarks>
public sealed record CompositeAlertEvaluationResult(
    IReadOnlyList<AlertRecord> Created,
    int SuppressedMatchCount);
