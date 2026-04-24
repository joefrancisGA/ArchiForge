using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Entry point for <c>archlucid trial smoke</c>. Wires up an <see cref="HttpClient" /> against the
///     resolved API base URL and delegates to <see cref="TrialSmokeRunner" />. Pure-HTTP loop — no docker,
///     no SQL on the developer machine — so it is safe to run against staging in Stripe TEST mode.
/// </summary>
[ExcludeFromCodeCoverage(Justification =
    "HTTP entry point; behaviour is covered by TrialSmokeRunnerTests + TrialSmokeCommandOptionsTests.")]
internal static class TrialSmokeCommand
{
    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> RunAsync(string[] args)
    {
        if (args is null) throw new ArgumentNullException(nameof(args));

        TrialSmokeCommandOptions? options = TrialSmokeCommandOptions.Parse(args, out string? error);

        if (options is null)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                "Usage: archlucid trial smoke --org <name> --email <email> [--display-name <name>] " +
                "[--baseline-hours <n>] [--baseline-source <text>] [--api-base-url <url>] [--skip-pilot-run-deltas] " +
                "[--staging] [--one-line]");

            return CliExitCode.UsageError;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = string.IsNullOrWhiteSpace(options.ApiBaseUrl)
            ? CliCommandShared.GetBaseUrl(config)
            : options.ApiBaseUrl!.Trim().TrimEnd('/');

        using HttpClient http = new() { BaseAddress = new Uri(baseUrl + "/") };
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        TrialSmokeRunner runner = new(http);
        TrialSmokeReport report = await runner.RunAsync(options);

        if (CliExecutionContext.JsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, JsonCamel));

            return report.AllPassed ? CliExitCode.Success : CliExitCode.OperationFailed;
        }

        if (options.OneLineOutput)
        {
            Console.WriteLine(TrialSmokeOneLineSummaryFormatter.Format(report, baseUrl));

            return report.AllPassed ? CliExitCode.Success : CliExitCode.OperationFailed;
        }

        Console.WriteLine($"archlucid trial smoke @ {baseUrl}");
        Console.WriteLine(new string('-', 60));

        foreach (TrialSmokeStepResult step in report.Steps)
        {
            string verdict = step.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{verdict}] {step.Name,-22} {step.Detail}");

            if (!step.Passed && !string.IsNullOrWhiteSpace(step.FailureHint))
                Console.WriteLine($"        hint: {step.FailureHint}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine(report.AllPassed
            ? $"PASS — tenant={report.TenantId} welcomeRun={report.TrialWelcomeRunId ?? "<none>"} correlation={report.RegistrationCorrelationId ?? "<none>"}"
            : "FAIL — see step output above. Re-run with --json for machine-readable output.");

        return report.AllPassed ? CliExitCode.Success : CliExitCode.OperationFailed;
    }
}
