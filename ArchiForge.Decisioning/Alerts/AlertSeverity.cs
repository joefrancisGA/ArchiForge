namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Canonical severity strings for <see cref="AlertRecord.Severity"/>, <see cref="AlertRule.Severity"/>, and routing thresholds.
/// </summary>
/// <remarks>Ordering for comparisons is defined by <c>AlertSeverityComparer</c> (<c>Alerts.Delivery</c>).</remarks>
public static class AlertSeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string High = "High";
    public const string Critical = "Critical";
}
