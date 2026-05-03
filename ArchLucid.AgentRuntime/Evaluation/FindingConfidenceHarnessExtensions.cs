using ArchLucid.Decisioning.Findings;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>Maps <see cref="AgentOutputHarnessResult" /> into <see cref="FindingConfidenceCalculator" /> inputs.</summary>
public static class FindingConfidenceHarnessExtensions
{
    /// <inheritdoc cref="FindingConfidenceCalculator.Calculate(bool,bool,System.Decimal?)" />
    public static FindingConfidenceCalculationResult? CalculateFromHarness(
        this FindingConfidenceCalculator calculator,
        AgentOutputHarnessResult? harnessResult,
        decimal? traceCompletenessRatio,
        bool referenceCaseMatched)
    {
        ArgumentNullException.ThrowIfNull(calculator);

        return calculator.Calculate(harnessResult?.Passed ?? false, referenceCaseMatched, traceCompletenessRatio);
    }
}
