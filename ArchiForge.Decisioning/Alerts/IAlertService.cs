namespace ArchiForge.Decisioning.Alerts;

public interface IAlertService
{
    Task<AlertEvaluationOutcome> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct);

    Task<AlertRecord?> ApplyActionAsync(
        Guid alertId,
        string userId,
        string userName,
        AlertActionRequest request,
        CancellationToken ct);
}
