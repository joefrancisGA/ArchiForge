using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Persistence.ContextSnapshots;

/// <summary>Dapper projection for <c>dbo.ContextSnapshots</c> header + legacy JSON columns.</summary>
[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class ContextSnapshotStorageRow
{
    public Guid SnapshotId
    {
        get;
        init;
    }

    public Guid RunId
    {
        get;
        init;
    }

    public string ProjectId
    {
        get;
        init;
    } = null!;

    public DateTime CreatedUtc
    {
        get;
        init;
    }

    public string CanonicalObjectsJson
    {
        get;
        init;
    } = null!;

    public string? DeltaSummary
    {
        get;
        init;
    }

    public string WarningsJson
    {
        get;
        init;
    } = null!;

    public string ErrorsJson
    {
        get;
        init;
    } = null!;

    public string SourceHashesJson
    {
        get;
        init;
    } = null!;
}
