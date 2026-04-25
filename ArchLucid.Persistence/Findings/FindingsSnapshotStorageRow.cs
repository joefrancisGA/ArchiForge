using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Persistence.Findings;

/// <summary>Dapper projection for <c>dbo.FindingsSnapshots</c> header + <c>FindingsJson</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class FindingsSnapshotStorageRow
{
    public Guid FindingsSnapshotId
    {
        get;
        init;
    }

    public Guid RunId
    {
        get;
        init;
    }

    public Guid ContextSnapshotId
    {
        get;
        init;
    }

    public Guid GraphSnapshotId
    {
        get;
        init;
    }

    public DateTime CreatedUtc
    {
        get;
        init;
    }

    public int SchemaVersion
    {
        get;
        init;
    }

    public string FindingsJson
    {
        get;
        init;
    } = null!;
}
