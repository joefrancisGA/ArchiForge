namespace ArchiForge.Persistence.Repositories;

/// <summary>Thrown when a <c>dbo.Runs</c> update affects zero rows because <c>RowVersionStamp</c> did not match (concurrent writer won).</summary>
public sealed class RunConcurrencyConflictException : InvalidOperationException
{
    /// <summary>Creates an exception for the given run id.</summary>
    public RunConcurrencyConflictException(Guid runId)
        : base($"Run '{runId:D}' was modified by another writer; reload and retry.")
    {
        RunId = runId;
    }

    /// <summary>Run that failed the optimistic concurrency check.</summary>
    public Guid RunId { get; }
}
