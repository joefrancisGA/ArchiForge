namespace ArchLucid.Persistence.Models;

public sealed class RunRecord
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    /// <summary>Scoped solution/project boundary (GUID). Distinct from <see cref="ProjectId"/> slug.</summary>
    public Guid ScopeProjectId { get; set; }
    public Guid RunId { get; set; }
    public string ProjectId { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    public Guid? GraphSnapshotId { get; set; }
    public Guid? FindingsSnapshotId { get; set; }
    public Guid? GoldenManifestId { get; set; }
    public Guid? DecisionTraceId { get; set; }
    public Guid? ArtifactBundleId { get; set; }

    /// <summary>FK logical key to <c>ArchitectureRequests.RequestId</c>.</summary>
    public string? ArchitectureRequestId { get; set; }

    /// <summary>String form of lifecycle enum (<c>ArchitectureRunStatus</c>) for API/read parity with legacy rows.</summary>
    public string? LegacyRunStatus { get; set; }

    /// <summary>UTC when the run reached a terminal lifecycle state.</summary>
    public DateTime? CompletedUtc { get; set; }

    /// <summary>Latest committed manifest version key.</summary>
    public string? CurrentManifestVersion { get; set; }

    /// <summary>W3C trace ID from <c>Activity.Current?.TraceId</c> at run creation; used for post-hoc trace lookup.</summary>
    public string? OtelTraceId { get; set; }

    /// <summary>When set, the run is excluded from list/detail authority APIs (soft archival).</summary>
    public DateTime? ArchivedUtc { get; set; }

    /// <summary>SQL Server <c>ROWVERSION</c> for optimistic concurrency on updates; <see langword="null"/> before first read/insert round-trip.</summary>
    public byte[]? RowVersion { get; set; }
}
