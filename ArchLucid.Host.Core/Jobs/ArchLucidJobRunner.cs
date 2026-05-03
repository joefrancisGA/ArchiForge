namespace ArchLucid.Host.Core.Jobs;

/// <summary>Resolves <see cref="IArchLucidJob"/> by slug and runs it with telemetry.</summary>
public sealed class ArchLucidJobRunner(
    IEnumerable<IArchLucidJob> jobs,
    JobRunTelemetry telemetry,
    ILogger<ArchLucidJobRunner> logger)
{
    private readonly IReadOnlyDictionary<string, IArchLucidJob> _jobsByName =
        (jobs ?? throw new ArgumentNullException(nameof(jobs)))
        .Where(static _ => true)
        .ToDictionary(static j => j.Name, static j => j, StringComparer.OrdinalIgnoreCase);

    private readonly JobRunTelemetry _telemetry =
        telemetry ?? throw new ArgumentNullException(nameof(telemetry));

    private readonly ILogger<ArchLucidJobRunner> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Runs the named job; returns <see cref="ArchLucidJobExitCodes.UnknownJob"/> when no implementation is registered.</summary>
    public Task<int> RunNamedJobAsync(string jobName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        if (_jobsByName.TryGetValue(jobName, out IArchLucidJob? job))
            return _telemetry.RunWithTelemetryAsync(jobName, ct => job.RunOnceAsync(ct), cancellationToken);
        _logger.LogError("Unknown job name: {JobName}", jobName);

        return Task.FromResult(ArchLucidJobExitCodes.UnknownJob);
    }
}
