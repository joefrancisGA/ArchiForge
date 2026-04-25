using ArchLucid.Contracts.Agents;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Persistence contract for <see cref="AgentExecutionTrace" /> records that capture
///     the step-by-step execution log of each agent during a run.
/// </summary>
public interface IAgentExecutionTraceRepository
{
    /// <summary>Persists a single execution trace entry.</summary>
    /// <param name="trace">The trace to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateAsync(
        AgentExecutionTrace trace,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates blob pointer columns and refreshes <c>TraceJson</c> after asynchronous full-text uploads.
    /// </summary>
    Task PatchBlobStorageFieldsAsync(
        string traceId,
        string? fullSystemPromptBlobKey,
        string? fullUserPromptBlobKey,
        string? fullResponseBlobKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the <see cref="AgentExecutionTrace.BlobUploadFailed" /> flag on a trace row.
    /// </summary>
    Task PatchBlobUploadFailedAsync(
        string traceId,
        bool failed,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Merges full prompt/response text into <see cref="AgentExecutionTrace" /> JSON and optional SQL inline columns
    ///     for forensic recovery when blob keys are missing. Non-null parameters overwrite; null leaves existing values.
    /// </summary>
    Task PatchInlinePromptFallbackAsync(
        string traceId,
        string? fullSystemPromptInline,
        string? fullUserPromptInline,
        string? fullResponseInline,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets <see cref="AgentExecutionTrace.InlineFallbackFailed" /> when mandatory inline forensic text could not be
    ///     stored or verified.
    /// </summary>
    Task PatchInlineFallbackFailedAsync(
        string traceId,
        bool failed,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single trace by id, or <see langword="null" /> when the row is missing.</summary>
    Task<AgentExecutionTrace?> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all traces for the specified run, ordered by <c>CreatedUtc</c> ascending.
    /// </summary>
    /// <param name="runId">The run whose traces are requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns a page of traces for the run ordered by <c>CreatedUtc</c> ascending,
    ///     together with the total row count for that run.
    /// </summary>
    /// <param name="runId">The run whose traces are requested.</param>
    /// <param name="offset">Zero-based row offset for paging.</param>
    /// <param name="limit">Maximum number of rows to return.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all traces associated with a specific agent task, ordered by <c>CreatedUtc</c> ascending.
    /// </summary>
    /// <param name="taskId">The agent task whose traces are requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default);
}
