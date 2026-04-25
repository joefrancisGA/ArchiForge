using System.Diagnostics.CodeAnalysis;

using ArchLucid.Persistence.Audit;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>
///     Runs the Cosmos change feed processor until the first non-empty handler batch completes, or
///     <paramref name="maxIdleWait" /> elapses (idle success).
/// </summary>
/// <remarks>
///     Uses the same processor name and lease container as <see cref="AuditEventChangeFeedHostedService" /> so checkpoints
///     are shared when operators migrate workloads.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires Cosmos account or emulator.")]
public sealed class AuditEventChangeFeedSingleBatchProcessor : IAuditEventChangeFeedSingleBatchRunner
{
    private readonly CosmosClientFactory _clientFactory;
    private readonly IAuditEventChangeFeedHandler _handler;
    private readonly ILogger<AuditEventChangeFeedSingleBatchProcessor> _logger;
    private readonly IOptionsMonitor<CosmosDbOptions> _optionsMonitor;

    public AuditEventChangeFeedSingleBatchProcessor(
        CosmosClientFactory clientFactory,
        IOptionsMonitor<CosmosDbOptions> optionsMonitor,
        IAuditEventChangeFeedHandler handler,
        ILogger<AuditEventChangeFeedSingleBatchProcessor> logger)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Waits for one handler batch or returns when <paramref name="maxIdleWait" /> passes with no changes.</summary>
    public async Task RunSingleBatchOrIdleAsync(TimeSpan maxIdleWait, CancellationToken cancellationToken)
    {
        CosmosDbOptions opts = _optionsMonitor.CurrentValue;

        if (!opts.AuditEventsEnabled)
            return;


        string instanceName = $"job-{Guid.NewGuid():N}";

        Container feed = await _clientFactory.GetContainerAsync("audit-events", cancellationToken)
            .ConfigureAwait(false);
        Container leases = await _clientFactory.GetContainerAsync("audit-events-leases", cancellationToken)
            .ConfigureAwait(false);

        TaskCompletionSource<int> batchGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ChangeFeedProcessor processor = feed
            .GetChangeFeedProcessorBuilder<AuditEventDocument>(
                "archlucid-audit-events",
                async (context, changes, token) =>
                {
                    _ = context;

                    if (changes.Count == 0)
                        return;


                    List<AuditEventDocument> batch = changes.ToList();

                    try
                    {
                        await _handler.HandleAsync(batch, token).ConfigureAwait(false);
                        batchGate.TrySetResult(1);
                    }
                    catch (Exception ex)
                    {
                        batchGate.TrySetException(ex);
                        throw;
                    }
                })
            .WithInstanceName(instanceName)
            .WithLeaseContainer(leases)
            .Build();

        await processor.StartAsync().ConfigureAwait(false);

        try
        {
            Task idle = Task.Delay(maxIdleWait, cancellationToken);
            Task winner = await Task.WhenAny(batchGate.Task, idle).ConfigureAwait(false);

            if (winner == batchGate.Task)

                await batchGate.Task.ConfigureAwait(false);
        }
        finally
        {
            await processor.StopAsync().ConfigureAwait(false);
        }

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Cosmos audit change feed single-batch processor stopped: InstanceName={InstanceName}",
                instanceName);
    }
}
