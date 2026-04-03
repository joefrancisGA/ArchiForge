namespace ArchiForge.Contracts.Evolution;

/// <summary>Lifecycle for a 60R candidate change set (human-in-the-loop; no auto-promotion).</summary>
public static class EvolutionCandidateChangeSetStatusValues
{
    public const string Draft = "Draft";

    public const string Simulated = "Simulated";

    public const string PendingHumanReview = "PendingHumanReview";

    public const string Declined = "Declined";

    public const string Archived = "Archived";
}
