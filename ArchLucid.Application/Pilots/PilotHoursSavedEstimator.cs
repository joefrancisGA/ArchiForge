namespace ArchLucid.Application.Pilots;
/// <summary>
///     Rough planning-only estimate of manual architecture/diligence hours avoided from cumulative instrumentation +
///     audit samples (not payroll, not billing).
/// </summary>
public static class PilotHoursSavedEstimator
{
    /// <summary>Displayed next to <see cref="Estimate"/> on proof surfaces.</summary>
    public const string Methodology = "Planning heuristic only: 2.0h per architecture run created + 0.05h per finding emitted + 0.02h per audited row sampled in the demo scope snapshot (process-life counters).";
    /// <summary>Returns a non-negative hours estimate.</summary>
    public static double Estimate(long runsCreatedTotal, IReadOnlyDictionary<string, long> findingsBySeverity, int auditRowCount)
    {
        ArgumentNullException.ThrowIfNull(findingsBySeverity);
        if (findingsBySeverity is null)
            throw new ArgumentNullException(nameof(findingsBySeverity));
        long findingsTotal = 0L;
        foreach (KeyValuePair<string, long> pair in findingsBySeverity)
        {
            if (pair.Value > 0)
                findingsTotal += pair.Value;
        }

        double raw = runsCreatedTotal * 2.0d + findingsTotal * 0.05d + auditRowCount * 0.02d;
        return raw < 0d ? 0d : raw;
    }
}