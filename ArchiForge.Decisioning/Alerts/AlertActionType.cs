namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Allowed values for <see cref="AlertActionRequest.Action"/>; mapped to new <see cref="AlertStatus"/> in persistence <c>AlertService.ApplyActionAsync</c>.
/// </summary>
public static class AlertActionType
{
    public const string Acknowledge = "Acknowledge";
    public const string Resolve = "Resolve";
    public const string Suppress = "Suppress";
}
