namespace ArchLucid.Application.Analysis;

/// <summary>
///     Thrown when replay mode is <see cref="ComparisonReplayMode.Verify" /> and the
///     regenerated comparison does not match the stored payload (engine or architecture drift).
/// </summary>
public sealed class ComparisonVerificationFailedException : InvalidOperationException
{
    public ComparisonVerificationFailedException(string message, DriftAnalysisResult? drift = null)
        : base(message)
    {
        Drift = drift;
    }

    public ComparisonVerificationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DriftAnalysisResult? Drift
    {
        get;
    }
}
