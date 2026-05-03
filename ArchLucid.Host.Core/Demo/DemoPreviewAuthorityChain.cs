namespace ArchLucid.Host.Core.Demo;

/// <summary>Authority chain identifiers (read-only mirror of operator run detail).</summary>
public sealed class DemoPreviewAuthorityChain
{
    public string? ContextSnapshotId
    {
        get;
        init;
    }

    public string? GraphSnapshotId
    {
        get;
        init;
    }

    public string? FindingsSnapshotId
    {
        get;
        init;
    }

    public string? GoldenManifestId
    {
        get;
        init;
    }

    public string? DecisionTraceId
    {
        get;
        init;
    }

    public string? ArtifactBundleId
    {
        get;
        init;
    }
}
