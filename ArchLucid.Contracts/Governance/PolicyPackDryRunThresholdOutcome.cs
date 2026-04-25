namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Per-threshold-key breach result for a single run inside a dry-run response. Numeric values are
///     transported as <see cref="double" /> so the same shape can carry "max severity count" thresholds
///     (integers) and "max time-to-commit minutes" thresholds (fractional minutes).
/// </summary>
public sealed class PolicyPackDryRunThresholdOutcome
{
    /// <summary>Threshold key (e.g. <c>maxCriticalFindings</c>); see <see cref="PolicyPackDryRunSupportedThresholdKeys" />.</summary>
    public string Key
    {
        get; init;
    } = string.Empty;

    /// <summary>Proposed numeric value for the threshold (parsed from the request body's redacted string value).</summary>
    public double ProposedValue
    {
        get; init;
    }

    /// <summary>The run's actual numeric metric value (e.g. critical findings count, time-to-commit minutes).</summary>
    public double ActualValue
    {
        get; init;
    }

    /// <summary>
    ///     <see langword="true" /> when <see cref="ActualValue" /> exceeds <see cref="ProposedValue" />
    ///     (or, for inverted thresholds, when the run violates the proposed cap). All V1 thresholds are
    ///     "actual must be ≤ proposed".
    /// </summary>
    public bool WouldBreach
    {
        get; init;
    }
}
