namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Pilot signal link backing an improvement plan.</summary>
public sealed class LearningPlanningReportSignalRef
{
    public Guid SignalId { get; init; }

    public string? TriageStatusSnapshot { get; init; }
}
