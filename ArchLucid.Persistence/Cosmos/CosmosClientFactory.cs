using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Lazy-initializes <see cref="CosmosClient" /> and shared containers for polyglot persistence.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires live Cosmos account or emulator.")]
public sealed class CosmosClientFactory : IDisposable
{
    private readonly ConcurrentDictionary<string, Container> _containers = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ILogger<CosmosClientFactory> _logger;
    private readonly IOptionsMonitor<CosmosDbOptions> _optionsMonitor;
    private CosmosClient? _client;
    private Database? _database;
    private bool _disposed;

    public CosmosClientFactory(IOptionsMonitor<CosmosDbOptions> optionsMonitor, ILogger<CosmosClientFactory> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _client?.Dispose();
        _initLock.Dispose();
    }

    public async Task<Container> GetContainerAsync(string containerId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        if (_containers.TryGetValue(containerId, out Container? cached))
            return cached;

        await _initLock.WaitAsync(ct);

        try
        {
            if (_containers.TryGetValue(containerId, out cached))
                return cached;

            CosmosDbOptions opts = _optionsMonitor.CurrentValue;

            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                throw new InvalidOperationException(
                    "CosmosDb:ConnectionString is required when Cosmos features are enabled.");

            _client ??= CreateClient(opts);
            _database ??= await _client.CreateDatabaseIfNotExistsAsync(opts.DatabaseName, cancellationToken: ct);

            int throughput = 400;
            ContainerProperties properties = new(containerId, GetPartitionKeyPath(containerId))
            {
                DefaultTimeToLive = GetDefaultTtl(containerId, opts)
            };

            ContainerResponse response = await _database.CreateContainerIfNotExistsAsync(
                properties,
                ThroughputProperties.CreateManualThroughput(throughput),
                cancellationToken: ct);

            Container container = response.Container;
            _containers[containerId] = container;

            return container;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<Container> GetLeaseContainerAsync(CancellationToken ct)
    {
        const string leaseContainerId = "audit-events-leases";

        if (_containers.TryGetValue(leaseContainerId, out Container? cached))
            return cached;

        await _initLock.WaitAsync(ct);

        try
        {
            if (_containers.TryGetValue(leaseContainerId, out cached))
                return cached;

            CosmosDbOptions opts = _optionsMonitor.CurrentValue;

            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                throw new InvalidOperationException(
                    "CosmosDb:ConnectionString is required when Cosmos features are enabled.");

            _client ??= CreateClient(opts);
            _database ??= await _client.CreateDatabaseIfNotExistsAsync(opts.DatabaseName, cancellationToken: ct);

            ContainerProperties leaseProps = new(leaseContainerId, "/id");
            ContainerResponse leaseResponse = await _database.CreateContainerIfNotExistsAsync(
                leaseProps,
                ThroughputProperties.CreateManualThroughput(400),
                cancellationToken: ct);

            Container lease = leaseResponse.Container;
            _containers[leaseContainerId] = lease;

            return lease;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static int? GetDefaultTtl(string containerId, CosmosDbOptions opts)
    {
        if (!string.Equals(containerId, "agent-traces", StringComparison.Ordinal))
            return null;

        int ttl = opts.AgentTraceTtlSeconds;

        if (ttl <= 0)
            return null;

        return ttl;
    }

    private static string GetPartitionKeyPath(string containerId)
    {
        if (string.Equals(containerId, "graph-snapshots", StringComparison.Ordinal))
            return "/graphSnapshotId";

        if (string.Equals(containerId, "agent-traces", StringComparison.Ordinal))
            return "/runId";

        return string.Equals(containerId, "audit-events", StringComparison.Ordinal) ? "/tenantId" : throw new ArgumentOutOfRangeException(nameof(containerId), containerId, "Unknown Cosmos container id.");
    }

    private CosmosClient CreateClient(CosmosDbOptions opts)
    {
        CosmosClientOptions clientOptions = new()
        {
            ApplicationName = "ArchLucid",
            ConnectionMode = ConnectionMode.Direct,
            ConsistencyLevel = ParseConsistency(opts.DefaultConsistencyLevel)
        };

        if (IsEmulatorConnection(opts.ConnectionString))

            // Emulator uses a self-signed certificate; safe only for localhost emulator endpoints.
            clientOptions.HttpClientFactory = () =>
            {
                HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
                };

                return new HttpClient(handler);
            };


        CosmosClient client = new(opts.ConnectionString, clientOptions);

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "CosmosClient initialized (database {Database}, consistency {Consistency}, emulator={Emulator}).",
                opts.DatabaseName,
                clientOptions.ConsistencyLevel,
                IsEmulatorConnection(opts.ConnectionString));


        return client;
    }

    internal static bool IsEmulatorConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        return connectionString.Contains("localhost:8081", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("127.0.0.1:8081", StringComparison.OrdinalIgnoreCase);
    }

    private static ConsistencyLevel ParseConsistency(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ConsistencyLevel.Session;

        return Enum.TryParse(raw.Trim(), true, out ConsistencyLevel level) ? level : ConsistencyLevel.Session;
    }
}
