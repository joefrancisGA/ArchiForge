namespace ArchLucid.Decisioning.Interfaces;

/// <summary>
///     Keying and rule-set identity for persisting a coordinator-shaped <c>Contracts.Manifest.GoldenManifest</c> as
///     an authority row.
/// </summary>
public sealed class SaveContractsManifestOptions
{
    public required Guid ManifestId
    {
        get;
        init;
    }

    public required Guid RunId
    {
        get;
        init;
    }

    public required Guid ContextSnapshotId
    {
        get;
        init;
    }

    public required Guid GraphSnapshotId
    {
        get;
        init;
    }

    public required Guid FindingsSnapshotId
    {
        get;
        init;
    }

    public required Guid DecisionTraceId
    {
        get;
        init;
    }

    public required string RuleSetId
    {
        get;
        init;
    }

    public required string RuleSetVersion
    {
        get;
        init;
    }

    public required string RuleSetHash
    {
        get;
        init;
    }

    public DateTime CreatedUtc
    {
        get;
        init;
    } = DateTime.UtcNow;
}
