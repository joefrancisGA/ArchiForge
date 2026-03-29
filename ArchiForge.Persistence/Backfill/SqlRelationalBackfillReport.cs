namespace ArchiForge.Persistence.Backfill;

public sealed class SqlRelationalBackfillReport
{
    public int ProcessedCount
    {
        get; set;
    }

    public int SuccessCount
    {
        get; set;
    }

    public int FailureCount
    {
        get; set;
    }

    public List<SqlRelationalBackfillFailure> Failures { get; } = [];
}

public sealed class SqlRelationalBackfillFailure
{
    public required string Stage
    {
        get; init;
    }

    public required string EntityKey
    {
        get; init;
    }

    public required string Message
    {
        get; init;
    }
}
