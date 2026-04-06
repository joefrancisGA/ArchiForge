namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Deterministic planning knobs (no runtime mutation of generation or evaluation).</summary>
public sealed class ImprovementPlanningOptions
{
    /// <summary>Version token embedded in deterministic <see cref="ImprovementPlan.PlanId"/> derivation.</summary>
    public string RuleVersion { get; init; } = "59R-plan-v1";

    /// <summary>Upper bound on steps per plan (minimum 1, maximum 20).</summary>
    public int MaxStepsPerPlan { get; init; } = 5;

    /// <summary>Optional fixed timestamp for tests; default uses <see cref="DateTime.UtcNow"/>.</summary>
    public DateTime? CreatedUtcOverride { get; init; }
}
