using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Cosmos-backed <see cref="IAgentExecutionTraceRepository"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires Cosmos account or emulator.")]
public sealed class CosmosAgentExecutionTraceRepository(
    CosmosClientFactory clientFactory,
    IOptionsMonitor<CosmosDbOptions> optionsMonitor) : IAgentExecutionTraceRepository
{
    private const string ContainerId = "agent-traces";

    private readonly CosmosClientFactory _clientFactory =
        clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

    private readonly IOptionsMonitor<CosmosDbOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public async Task CreateAsync(AgentExecutionTrace trace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);

        Container container = await _clientFactory.GetContainerAsync(ContainerId, cancellationToken);
        CosmosDbOptions opts = _optionsMonitor.CurrentValue;
        string json = JsonSerializer.Serialize(trace, ContractJson.Default);
        int? ttl = opts.AgentTraceTtlSeconds > 0 ? opts.AgentTraceTtlSeconds : null;

        AgentTraceDocument doc = new()
        {
            Id = trace.TraceId,
            RunId = trace.RunId,
            TraceJson = json,
            CreatedUtc = trace.CreatedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            TaskId = trace.TaskId,
            Ttl = ttl,
        };

        await container.CreateItemAsync(doc, new PartitionKey(trace.RunId), cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchBlobStorageFieldsAsync(
        string traceId,
        string? fullSystemPromptBlobKey,
        string? fullUserPromptBlobKey,
        string? fullResponseBlobKey,
        CancellationToken cancellationToken = default)
    {
        AgentExecutionTrace? trace = await LoadTraceAsync(traceId, cancellationToken);

        if (trace is null)
            return;

        trace.FullSystemPromptBlobKey = fullSystemPromptBlobKey ?? trace.FullSystemPromptBlobKey;
        trace.FullUserPromptBlobKey = fullUserPromptBlobKey ?? trace.FullUserPromptBlobKey;
        trace.FullResponseBlobKey = fullResponseBlobKey ?? trace.FullResponseBlobKey;
        await ReplaceTraceAsync(trace, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchBlobUploadFailedAsync(string traceId, bool failed, CancellationToken cancellationToken = default)
    {
        AgentExecutionTrace? trace = await LoadTraceAsync(traceId, cancellationToken);

        if (trace is null)
            return;

        trace.BlobUploadFailed = failed ? true : null;
        await ReplaceTraceAsync(trace, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchInlinePromptFallbackAsync(
        string traceId,
        string? fullSystemPromptInline,
        string? fullUserPromptInline,
        string? fullResponseInline,
        CancellationToken cancellationToken = default)
    {
        AgentExecutionTrace? trace = await LoadTraceAsync(traceId, cancellationToken);

        if (trace is null)
            return;

        if (fullSystemPromptInline is not null)
            trace.FullSystemPromptInline = fullSystemPromptInline;

        if (fullUserPromptInline is not null)
            trace.FullUserPromptInline = fullUserPromptInline;

        if (fullResponseInline is not null)
            trace.FullResponseInline = fullResponseInline;

        await ReplaceTraceAsync(trace, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchInlineFallbackFailedAsync(string traceId, bool failed, CancellationToken cancellationToken = default)
    {
        AgentExecutionTrace? trace = await LoadTraceAsync(traceId, cancellationToken);

        if (trace is null)
            return;

        trace.InlineFallbackFailed = failed ? true : null;
        await ReplaceTraceAsync(trace, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentExecutionTrace?> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        AgentTraceDocument? doc = await FindDocumentByTraceIdAsync(traceId, cancellationToken);

        return doc is null ? null : Deserialize(doc);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<AgentExecutionTrace> traces, _) = await QueryRunPageAsync(runId, 0, 500, cancellationToken);

        return traces;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await QueryRunPageAsync(runId, offset, limit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        Container container = await _clientFactory.GetContainerAsync(ContainerId, cancellationToken);
        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.taskId = @taskId ORDER BY c.createdUtc")
            .WithParameter("@taskId", taskId);

        using FeedIterator<AgentTraceDocument> iterator = container.GetItemQueryIterator<AgentTraceDocument>(query);
        List<AgentExecutionTrace> list = [];

        while (iterator.HasMoreResults)
        {
            FeedResponse<AgentTraceDocument> page = await iterator.ReadNextAsync(cancellationToken);

            foreach (AgentTraceDocument doc in page)
            {
                list.Add(Deserialize(doc));
            }
        }

        return list;
    }

    private async Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> QueryRunPageAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken ct)
    {
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        int clampedOffset = Math.Max(0, offset);
        int clampedLimit = Math.Clamp(limit, 1, 500);

        QueryDefinition countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.runId = @runId")
            .WithParameter("@runId", runId);

        int total = 0;
        using FeedIterator<int> countIt = container.GetItemQueryIterator<int>(
            countQuery,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(runId) });

        if (countIt.HasMoreResults)
        {
            FeedResponse<int> countPage = await countIt.ReadNextAsync(ct);
            total = countPage.Resource.FirstOrDefault();
        }

        QueryDefinition pageQuery = new QueryDefinition(
                """
                SELECT * FROM c
                WHERE c.runId = @runId
                ORDER BY c.createdUtc
                OFFSET @off LIMIT @lim
                """)
            .WithParameter("@runId", runId)
            .WithParameter("@off", clampedOffset)
            .WithParameter("@lim", clampedLimit);

        using FeedIterator<AgentTraceDocument> iterator = container.GetItemQueryIterator<AgentTraceDocument>(
            pageQuery,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(runId) });

        List<AgentExecutionTrace> traces = [];

        while (iterator.HasMoreResults)
        {
            FeedResponse<AgentTraceDocument> page = await iterator.ReadNextAsync(ct);

            foreach (AgentTraceDocument doc in page)
            {
                traces.Add(Deserialize(doc));
            }
        }

        return (traces, total);
    }

    private async Task ReplaceTraceAsync(AgentExecutionTrace trace, CancellationToken ct)
    {
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        CosmosDbOptions opts = _optionsMonitor.CurrentValue;
        int? ttl = opts.AgentTraceTtlSeconds > 0 ? opts.AgentTraceTtlSeconds : null;

        AgentTraceDocument doc = new()
        {
            Id = trace.TraceId,
            RunId = trace.RunId,
            TraceJson = JsonSerializer.Serialize(trace, ContractJson.Default),
            CreatedUtc = trace.CreatedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            TaskId = trace.TaskId,
            Ttl = ttl,
        };

        await container.ReplaceItemAsync(doc, trace.TraceId, new PartitionKey(trace.RunId), cancellationToken: ct);
    }

    private async Task<AgentExecutionTrace?> LoadTraceAsync(string traceId, CancellationToken ct)
    {
        AgentTraceDocument? doc = await FindDocumentByTraceIdAsync(traceId, ct);

        return doc is null ? null : Deserialize(doc);
    }

    private async Task<AgentTraceDocument?> FindDocumentByTraceIdAsync(string traceId, CancellationToken ct)
    {
        Container container = await _clientFactory.GetContainerAsync(ContainerId, ct);
        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", traceId);

        using FeedIterator<AgentTraceDocument> iterator = container.GetItemQueryIterator<AgentTraceDocument>(query);

        while (iterator.HasMoreResults)
        {
            FeedResponse<AgentTraceDocument> page = await iterator.ReadNextAsync(ct);
            AgentTraceDocument? doc = page.Resource.FirstOrDefault();

            if (doc is not null)
                return doc;
        }

        return null;
    }

    private static AgentExecutionTrace Deserialize(AgentTraceDocument doc)
    {
        return JsonSerializer.Deserialize<AgentExecutionTrace>(doc.TraceJson, ContractJson.Default)
               ?? throw new InvalidOperationException("Trace document deserialized to null.");
    }
}
