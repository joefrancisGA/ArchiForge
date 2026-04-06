namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Lifecycle strings stored on <see cref="AlertRecord.Status"/> and used in SQL deduplication filters.
/// </summary>
public static class AlertStatus
{
    public const string Open = "Open";
    public const string Acknowledged = "Acknowledged";
    public const string Resolved = "Resolved";
    public const string Suppressed = "Suppressed";
}
