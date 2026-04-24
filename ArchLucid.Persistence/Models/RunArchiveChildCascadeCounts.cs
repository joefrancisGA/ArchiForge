namespace ArchLucid.Persistence.Models;

/// <summary>
///     Row counts for soft-archive cascade when bulk-archiving <c>dbo.Runs</c> (per batch, not per run).
/// </summary>
public sealed record RunArchiveChildCascadeCounts
{
    public int GoldenManifests
    {
        get;
        init;
    }

    public int FindingsSnapshots
    {
        get;
        init;
    }

    public int ContextSnapshots
    {
        get;
        init;
    }

    public int GraphSnapshots
    {
        get;
        init;
    }

    public int DecisioningTraces
    {
        get;
        init;
    }

    public int ArtifactBundles
    {
        get;
        init;
    }

    public int AgentExecutionTraces
    {
        get;
        init;
    }

    public int ComparisonRecords
    {
        get;
        init;
    }
}
