namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Maps explainability trace completeness ratios to operator-facing labels.
/// </summary>
public static class TraceConfidenceLabels
{
    public const string High = "High";

    public const string Medium = "Medium";

    public const string Low = "Low";

    public static string FromCompletenessRatio(double completenessRatio)
    {
        if (completenessRatio >= 0.8 - 1e-9)
            return High;

        return completenessRatio >= 0.5 - 1e-9 ? Medium : Low;
    }
}
