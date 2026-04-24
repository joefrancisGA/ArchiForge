using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Persistence.Coordination.Backfill;

/// <summary>Which authority JSON payloads to scan during a one-time relational backfill.</summary>
[ExcludeFromCodeCoverage(Justification = "Backfill options DTO; no logic.")]
public sealed class SqlRelationalBackfillOptions
{
    public bool ContextSnapshots
    {
        get;
        init;
    } = true;

    public bool GraphSnapshots
    {
        get;
        init;
    } = true;

    public bool FindingsSnapshots
    {
        get;
        init;
    } = true;

    public bool GoldenManifestsPhase1
    {
        get;
        init;
    } = true;

    public bool ArtifactBundles
    {
        get;
        init;
    } = true;
}
