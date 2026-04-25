using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Persistence.Coordination.Backfill;

[ExcludeFromCodeCoverage(Justification = "Backfill report DTO; no logic.")]
public sealed class SqlRelationalBackfillReport
{
    public int ProcessedCount
    {
        get;
        set;
    }

    public int SuccessCount
    {
        get;
        set;
    }

    public int FailureCount
    {
        get;
        set;
    }

    public List<SqlRelationalBackfillFailure> Failures
    {
        get;
    } = [];
}

[ExcludeFromCodeCoverage(Justification = "Backfill failure row DTO; no logic.")]
public sealed class SqlRelationalBackfillFailure
{
    public required string Stage
    {
        get;
        init;
    }

    public required string EntityKey
    {
        get;
        init;
    }

    public required string Message
    {
        get;
        init;
    }
}
