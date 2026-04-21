namespace ArchLucid.Cli.Commands;

/// <summary>
/// Result of a single step in <c>archlucid trial smoke</c>. Captured per step so test code can assert
/// on PASS / FAIL outcomes deterministically without parsing console output.
/// </summary>
public sealed class TrialSmokeStepResult
{
    public string Name
    {
        get; init;
    } = string.Empty;

    public bool Passed
    {
        get; init;
    }

    public string Detail
    {
        get; init;
    } = string.Empty;

    /// <summary>
    /// Forensic hint pointing at where to look in the audit chain when the step fails (e.g. an audit
    /// event type or a runbook anchor). Null on success.
    /// </summary>
    public string? FailureHint
    {
        get; init;
    }
}
