namespace ArchLucid.Cli.Commands;

/// <summary>Summary returned by <see cref="TrialSmokeRunner.RunAsync" />; per-step details + overall verdict.</summary>
public sealed class TrialSmokeReport
{
    public IReadOnlyList<TrialSmokeStepResult> Steps
    {
        get;
        init;
    } = [];

    public bool AllPassed
    {
        get;
        init;
    }

    public string? TenantId
    {
        get;
        init;
    }

    public string? TrialWelcomeRunId
    {
        get;
        init;
    }

    /// <summary>
    ///     <c>X-Correlation-ID</c> response header observed on the first <c>POST /v1/register</c> call.
    ///     Used by <c>--staging</c> / <c>--one-line</c> output so an oncall responder has a single id
    ///     to grep across logs / audit events.
    /// </summary>
    public string? RegistrationCorrelationId
    {
        get;
        init;
    }
}
