namespace ArchLucid.Cli.Commands;

/// <summary>Summary returned by <see cref="TrialSmokeRunner.RunAsync"/>; per-step details + overall verdict.</summary>
public sealed class TrialSmokeReport
{
    public IReadOnlyList<TrialSmokeStepResult> Steps
    {
        get; init;
    } = [];

    public bool AllPassed
    {
        get; init;
    }

    public string? TenantId
    {
        get; init;
    }

    public string? TrialWelcomeRunId
    {
        get; init;
    }
}
