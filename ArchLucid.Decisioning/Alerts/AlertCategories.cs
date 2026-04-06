namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Canonical category strings written to <see cref="AlertRecord.Category"/> by <see cref="AlertEvaluator"/>.
/// Using these constants prevents routing mismatches when delivery channels filter by category.
/// </summary>
public static class AlertCategories
{
    public const string Advisory = "Advisory";
    public const string Compliance = "Compliance";
    public const string Security = "Security";
    public const string Cost = "Cost";
    public const string Recommendation = "Recommendation";
    public const string Learning = "Learning";
    public const string CompositeAlert = "CompositeAlert";
}
