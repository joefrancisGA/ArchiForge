namespace ArchiForge.Decisioning.Alerts;

/// <summary>HTTP body for <c>POST …/alerts/{id}/action</c> (acknowledge, resolve, suppress).</summary>
/// <remarks>
/// <see cref="Action"/> must be a <see cref="AlertActionType"/> value (case-sensitive match in application service).
/// </remarks>
public class AlertActionRequest
{
    /// <summary>E.g. <see cref="AlertActionType.Acknowledge"/>.</summary>
    public string Action { get; set; } = null!;

    /// <summary>Optional operator comment stored on the alert for resolve/suppress.</summary>
    public string? Comment
    {
        get; set;
    }
}
