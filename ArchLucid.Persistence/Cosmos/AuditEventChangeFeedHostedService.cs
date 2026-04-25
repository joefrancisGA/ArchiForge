using System.Diagnostics.CodeAnalysis;

using ArchLucid.Persistence.Audit;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>
///     Runs the Cosmos change feed processor for <c>audit-events</c> when
///     <see cref="CosmosDbOptions.AuditEventsEnabled" /> is on.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires Cosmos account or emulator.")]
public sealed class AuditEventChangeFeedHostedService : BackgroundService
{
    private readonly CosmosClientFactory _clientFactory;
    private readonly IAuditEventChangeFeedHandler _handler;
    private readonly ILogger<AuditEventChangeFeedHostedService> _logger;
    private readonly IOptionsMonitor<CosmosDbOptions> _optionsMonitor;

    public AuditEventChangeFeedHostedService(
        CosmosClientFactory clientFactory,
        IOptionsMonitor<CosmosDbOptions> optionsMonitor,
        IAuditEventChangeFeedHandler handler,
        ILogger<AuditEventChangeFeedHostedService> logger)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CosmosDbOptions opts = _optionsMonitor.CurrentValue;

        if (!opts.AuditEventsEnabled)
            return;

        string instanceName = string.IsNullOrWhiteSpace(opts.ChangeFeedInstanceName)
            ? Environment.MachineName
            : opts.ChangeFeedInstanceName.Trim();

        Container feed = await _clientFactory.GetContainerAsync("audit-events", stoppingToken);
        Container leases = await _clientFactory.GetContainerAsync("audit-events-leases", stoppingToken);

        ChangeFeedProcessor processor = feed
            .GetChangeFeedProcessorBuilder<AuditEventDocument>(
                "archlucid-audit-events",
                OnChangesAsync)
            .WithInstanceName(instanceName)
            .WithLeaseContainer(leases)
            .Build();

        await processor.StartAsync();

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Cosmos audit change feed processor started: InstanceName={InstanceName}",
                instanceName);


        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        finally
        {
            await processor.StopAsync();
        }
    }

    private async Task OnChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<AuditEventDocument> changes,
        CancellationToken cancellationToken)
    {
        _ = context;

        if (changes.Count == 0)
            return;

        List<AuditEventDocument> batch = changes.ToList();

        await _handler.HandleAsync(batch, cancellationToken);
    }
}
