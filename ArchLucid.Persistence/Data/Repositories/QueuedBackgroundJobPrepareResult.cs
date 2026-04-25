namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Result of aligning a queue notification with <c>dbo.BackgroundJobs</c> under a row lock (multi-worker safe).
/// </summary>
/// <param name="ShouldRunExecutor">
///     When true, the worker should execute the job body and delete the queue message after
///     terminal persistence.
/// </param>
/// <param name="ShouldDeleteQueueMessageImmediately">
///     When true, delete the Azure queue message now (unknown id, terminal row, or duplicate notify while <c>Running</c>).
/// </param>
/// <param name="WasUnknownJobId">True when no <c>dbo.BackgroundJobs</c> row existed (stale queue message).</param>
/// <param name="RowWhenRunnable">
///     Populated when <paramref name="ShouldRunExecutor" /> is true; includes state transitioned
///     to Running.
/// </param>
public sealed record QueuedBackgroundJobPrepareResult(
    bool ShouldRunExecutor,
    bool ShouldDeleteQueueMessageImmediately,
    bool WasUnknownJobId,
    BackgroundJobRow? RowWhenRunnable);
