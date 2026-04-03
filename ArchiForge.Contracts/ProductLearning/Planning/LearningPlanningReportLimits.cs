namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Bounds for 59R planning report materialization (themes, plans, and per-plan evidence lists).</summary>
public sealed class LearningPlanningReportLimits
{
    public int MaxThemes { get; init; }

    public int MaxPlans { get; init; }

    public int MaxSignalRefsPerPlan { get; init; }

    public int MaxArtifactRefsPerPlan { get; init; }

    public int MaxRunRefsPerPlan { get; init; }
}
