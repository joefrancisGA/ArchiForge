namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     Projects <see cref="AlertEvaluationContext" /> into a flat <see cref="AlertMetricSnapshot" /> for composite rule
///     evaluation.
/// </summary>
public interface IAlertMetricSnapshotBuilder
{
    /// <summary>
    ///     Computes all metrics in one pass; safe when plan or comparison is null (missing facets default to 0).
    /// </summary>
    /// <param name="context">Same context passed to simple alert evaluation.</param>
    AlertMetricSnapshot Build(AlertEvaluationContext context);
}
