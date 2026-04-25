namespace ArchLucid.Decisioning.Alerts.Tuning;

/// <summary>Decomposed heuristic score from <see cref="AlertNoiseScorer" /> (higher <see cref="FinalScore" /> is better).</summary>
public class NoiseScoreBreakdown
{
    /// <summary>Reward for having some rule matches.</summary>
    public double CoverageScore
    {
        get;
        set;
    }

    /// <summary>Penalty when “would create” is outside the target band.</summary>
    public double NoisePenalty
    {
        get;
        set;
    }

    /// <summary>Penalty when many matches are suppressed.</summary>
    public double SuppressionPenalty
    {
        get;
        set;
    }

    /// <summary>Penalty when alert density exceeds one per evaluated run.</summary>
    public double DensityPenalty
    {
        get;
        set;
    }

    /// <summary><see cref="CoverageScore" /> minus penalties.</summary>
    public double FinalScore
    {
        get;
        set;
    }

    /// <summary>Human-readable scoring trace.</summary>
    public List<string> Notes
    {
        get;
        set;
    } = [];
}
