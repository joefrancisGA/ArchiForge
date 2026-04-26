namespace ArchLucid.Cli.Commands;

/// <summary>
///     Formats a <see cref="TrialSmokeReport" /> as a single greppable line for <c>--staging</c> /
///     <c>--one-line</c> output. Pure formatting (no I/O) so it is unit-testable and can be reused
///     by future tooling (e.g. webhook payload builders).
///     <para>
///         Format: <c>PASS|FAIL host=&lt;url&gt; correlation=&lt;id&gt; tenant=&lt;id&gt; welcomeRun=&lt;id|none&gt; failed=&lt;step&gt;</c>.
///     </para>
///     The fields are deliberately positional + space-delimited so an oncall responder reading a CI log line
///     or a webhook payload can grep on <c>correlation=</c> directly without parsing JSON.
/// </summary>
public static class TrialSmokeOneLineSummaryFormatter
{
    private const string NoneToken = "<none>";

    public static string Format(TrialSmokeReport report, string baseUrl)
    {
        if (report is null)
            throw new ArgumentNullException(nameof(report));
        if (baseUrl is null)
            throw new ArgumentNullException(nameof(baseUrl));

        string verdict = report.AllPassed ? "PASS" : "FAIL";
        string correlation = OrNone(report.RegistrationCorrelationId);
        string tenant = OrNone(report.TenantId);
        string welcomeRun = OrNone(report.TrialWelcomeRunId);
        string failedStep = report.AllPassed
            ? NoneToken
            : (FirstFailedStepName(report) ?? "<unknown>");

        return $"{verdict} host={baseUrl} correlation={correlation} tenant={tenant} welcomeRun={welcomeRun} failed={failedStep}";
    }

    private static string OrNone(string? value) => string.IsNullOrWhiteSpace(value) ? NoneToken : value;

    private static string? FirstFailedStepName(TrialSmokeReport report)
    {
        return (from step in report.Steps where !step.Passed select step.Name).FirstOrDefault();
    }
}
