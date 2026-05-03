using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.Archival;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>One archival pass via <see cref="DataArchivalHostIteration"/> (same body as <see cref="DataArchivalHostedService"/> loop).</summary>
public sealed class DataArchivalArchLucidJob(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<DataArchivalOptions> optionsMonitor,
    DataArchivalHostHealthState healthState,
    ILogger<DataArchivalArchLucidJob> logger) : IArchLucidJob
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly IOptionsMonitor<DataArchivalOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly DataArchivalHostHealthState _healthState =
        healthState ?? throw new ArgumentNullException(nameof(healthState));

    private readonly ILogger<DataArchivalArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.DataArchival;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            bool ok = await DataArchivalHostIteration.RunOnceAsync(
                    _scopeFactory,
                    _optionsMonitor.CurrentValue,
                    _logger,
                    _healthState,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!ok)
                return ArchLucidJobExitCodes.JobFailure;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data archival job failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
