using System.Diagnostics;
using System.Globalization;

using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>Structured start/end logging for one-shot jobs (Log Analytics / Application Insights queries).</summary>
public sealed class JobRunTelemetry(ILogger<JobRunTelemetry> logger)
{
    private readonly ILogger<JobRunTelemetry> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Runs <paramref name="execute"/> between <c>JobStarted</c> / <c>JobCompleted</c> log pairs.</summary>
    public async Task<int> RunWithTelemetryAsync(string jobName, Func<CancellationToken, Task<int>> execute, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentNullException.ThrowIfNull(execute);

        Stopwatch sw = Stopwatch.StartNew();

        _logger.LogInformation("JobStarted: JobName={JobName}", jobName);

        try
        {
            int exitCode = await execute(cancellationToken).ConfigureAwait(false);

            RecordJobOutcome(jobName, exitCode, sw.Elapsed.TotalMilliseconds, cancelled: false);

            _logger.LogInformation(
                "JobCompleted: JobName={JobName}, ExitCode={ExitCode}, DurationMs={DurationMs}",
                jobName,
                exitCode,
                sw.ElapsedMilliseconds);

            return exitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RecordJobOutcome(jobName, ArchLucidJobExitCodes.JobFailure, sw.Elapsed.TotalMilliseconds, cancelled: true);

            _logger.LogWarning(
                "JobCancelled: JobName={JobName}, DurationMs={DurationMs}",
                jobName,
                sw.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            RecordJobOutcome(jobName, ArchLucidJobExitCodes.JobFailure, sw.Elapsed.TotalMilliseconds, cancelled: false);

            _logger.LogError(
                ex,
                "JobFailed: JobName={JobName}, DurationMs={DurationMs}",
                jobName,
                sw.ElapsedMilliseconds);

            return ArchLucidJobExitCodes.JobFailure;
        }
    }

    private static void RecordJobOutcome(string jobName, int exitCode, double durationMs, bool cancelled)
    {
        string exitClass = cancelled
            ? "cancelled"
            : exitCode == ArchLucidJobExitCodes.Success
                ? "success"
                : exitCode == ArchLucidJobExitCodes.UnknownJob
                    ? "unknown_job"
                    : exitCode == ArchLucidJobExitCodes.ConfigurationError
                        ? "configuration_error"
                        : "failure";

        ArchLucidInstrumentation.ContainerJobRunsTotal.Add(
            1,
            new KeyValuePair<string, object?>("job_name", jobName),
            new KeyValuePair<string, object?>("exit_class", exitClass));

        ArchLucidInstrumentation.ContainerJobRunDurationMilliseconds.Record(
            durationMs,
            new KeyValuePair<string, object?>("job_name", jobName),
            new KeyValuePair<string, object?>(
                "exit_code",
                exitCode.ToString(CultureInfo.InvariantCulture)));
    }
}
