namespace ArchLucid.Api.Services.Admin;

/// <summary>Detection-only orphan row counts (same queries as the background orphan probe).</summary>
public sealed record DataConsistencyOrphanCounts(
    long ComparisonRecordsLeftRunIdOrphans,
    long ComparisonRecordsRightRunIdOrphans,
    long GoldenManifestsRunIdOrphans,
    long FindingsSnapshotsRunIdOrphans);
