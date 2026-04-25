namespace ArchLucid.Core.GoldenCorpus;

/// <summary>Aggregated outcome of <see cref="RealLlmOutputStructuralValidator.ValidateAgentResultStructure" />.</summary>
public sealed record RealLlmStructuralValidationResult(bool IsValid, IReadOnlyList<RealLlmStructuralCheckItem> Checks)
{
    /// <summary>True when every <see cref="Checks" /> item has <see cref="RealLlmStructuralCheckItem.Passed" /> set.</summary>
    public static bool AllPassed(IReadOnlyList<RealLlmStructuralCheckItem> checks) =>
        checks is { Count: > 0 } && checks.All(static c => c.Passed);
}
