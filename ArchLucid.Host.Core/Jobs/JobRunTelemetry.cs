using System.Diagnostics;

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

            _logger.LogInformation(
                "JobCompleted: JobName={JobName}, ExitCode={ExitCode}, DurationMs={DurationMs}",
                jobName,
                exitCode,
                sw.ElapsedMilliseconds);

            return exitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "JobCancelled: JobName={JobName}, DurationMs={DurationMs}",
                jobName,
                sw.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "JobFailed: JobName={JobName}, DurationMs={DurationMs}",
                jobName,
                sw.ElapsedMilliseconds);

            return ArchLucidJobExitCodes.JobFailure;
        }
    }
}
