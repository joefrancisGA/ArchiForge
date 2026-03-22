namespace ArchiForge.Decisioning.Alerts;

public interface IAlertEvaluator
{
    IReadOnlyList<AlertRecord> Evaluate(
        IReadOnlyList<AlertRule> rules,
        AlertEvaluationContext context);
}
