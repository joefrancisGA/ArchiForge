namespace ArchLucid.Persistence.Sql;

/// <summary>
///     Canonical SQL text for high-volume list/search paths (<c>dbo.Runs</c>, <c>dbo.AuditEvents</c>).
///     Unit tests assert shapes stay index-friendly without opening SQL connections.
/// </summary>
/// <remarks>
///     When changing repository queries, update these constants in the same PR and extend test assertions if new
///     predicates are required — see <c>docs/library/PERFORMANCE_BASELINES.md</c>.
/// </remarks>
public static class HotPathRelationalQueryShapes
{
    /// <summary>Dashboard run list by project slug (<c>SqlRunRepository.ListByProjectAsync</c>).</summary>
    public const string RunsListByProjectNoLock = """
                                                  SELECT TOP (@Take)
                                                      RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                                                      ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                                                      GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                                                      ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId,
                                                      IsPublicShowcase, RealModeFellBackToSimulator, PilotAoaiDeploymentSnapshot,
                                                      RetryCount, LastFailureReason
                                                  FROM dbo.Runs WITH (NOLOCK)
                                                  WHERE ProjectId = @ProjectSlug
                                                    AND TenantId = @TenantId
                                                    AND WorkspaceId = @WorkspaceId
                                                    AND ScopeProjectId = @ScopeProjectId
                                                    AND ArchivedUtc IS NULL
                                                  ORDER BY CreatedUtc DESC;
                                                  """;

    /// <summary>Keyset-paged run list by project (<c>SqlRunRepository.ListByProjectKeysetAsync</c>).</summary>
    public const string RunsListByProjectKeysetNoLock = """
                                                        SELECT TOP (@Fetch)
                                                            RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                                                            ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                                                            GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                                                            ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId,
                                                            IsPublicShowcase, RealModeFellBackToSimulator, PilotAoaiDeploymentSnapshot,
                                                            RetryCount, LastFailureReason
                                                        FROM dbo.Runs WITH (NOLOCK)
                                                        WHERE ProjectId = @ProjectSlug
                                                          AND TenantId = @TenantId
                                                          AND WorkspaceId = @WorkspaceId
                                                          AND ScopeProjectId = @ScopeProjectId
                                                          AND ArchivedUtc IS NULL
                                                          AND (
                                                              (@CursorRunId IS NULL AND @CursorCreatedUtc IS NULL)
                                                              OR CreatedUtc < @CursorCreatedUtc
                                                              OR (CreatedUtc = @CursorCreatedUtc AND RunId < @CursorRunId)
                                                          )
                                                        ORDER BY CreatedUtc DESC, RunId DESC;
                                                        """;

    /// <summary>
    ///     EXISTS predicate for committed architecture reviews with persisted golden manifests (nav narrowing signal).
    /// </summary>
    public const string CommittedArchitectureReviewExistsNoLock = """
                                                                      SELECT CASE WHEN EXISTS (
                                                                          SELECT 1
                                                                          FROM dbo.Runs r WITH (NOLOCK)
                                                                          INNER JOIN dbo.GoldenManifests gm WITH (NOLOCK)
                                                                              ON gm.ManifestId = r.GoldenManifestId AND gm.TenantId = r.TenantId
                                                                          WHERE r.TenantId = @TenantId
                                                                            AND r.WorkspaceId = @WorkspaceId
                                                                            AND r.ScopeProjectId = @ScopeProjectId
                                                                            AND r.ArchivedUtc IS NULL
                                                                            AND gm.ArchivedUtc IS NULL
                                                                            AND r.LegacyRunStatus = @CommittedStatus
                                                                            AND r.GoldenManifestId IS NOT NULL
                                                                      ) THEN 1 ELSE 0 END;
                                                                      """;

    /// <summary>Recent runs in ambient scope (<c>SqlRunRepository.ListRecentInScopeAsync</c>).</summary>
    public const string RunsListRecentInScopeNoLock = """
                                                      SELECT TOP (@Take)
                                                          RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                                                          ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                                                          GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                                                          ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId,
                                                          IsPublicShowcase, RealModeFellBackToSimulator, PilotAoaiDeploymentSnapshot,
                                                          RetryCount, LastFailureReason
                                                      FROM dbo.Runs WITH (NOLOCK)
                                                      WHERE TenantId = @TenantId
                                                        AND WorkspaceId = @WorkspaceId
                                                        AND ScopeProjectId = @ScopeProjectId
                                                        AND ArchivedUtc IS NULL
                                                      ORDER BY CreatedUtc DESC;
                                                      """;

    /// <summary>Keyset recent runs in scope (<c>SqlRunRepository.ListRecentInScopeKeysetAsync</c>).</summary>
    public const string RunsListRecentInScopeKeysetNoLock = """
                                                            SELECT TOP (@Fetch)
                                                                RunId, TenantId, WorkspaceId, ScopeProjectId, ProjectId, Description, CreatedUtc,
                                                                ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId,
                                                                GoldenManifestId, DecisionTraceId, ArtifactBundleId, ArchivedUtc,
                                                                ArchitectureRequestId, LegacyRunStatus, CompletedUtc, CurrentManifestVersion, OtelTraceId,
                                                                IsPublicShowcase, RealModeFellBackToSimulator, PilotAoaiDeploymentSnapshot,
                                                                RetryCount, LastFailureReason
                                                            FROM dbo.Runs WITH (NOLOCK)
                                                            WHERE TenantId = @TenantId
                                                              AND WorkspaceId = @WorkspaceId
                                                              AND ScopeProjectId = @ScopeProjectId
                                                              AND ArchivedUtc IS NULL
                                                              AND (
                                                                  (@CursorRunId IS NULL AND @CursorCreatedUtc IS NULL)
                                                                  OR CreatedUtc < @CursorCreatedUtc
                                                                  OR (CreatedUtc = @CursorCreatedUtc AND RunId < @CursorRunId)
                                                              )
                                                            ORDER BY CreatedUtc DESC, RunId DESC;
                                                            """;

    /// <summary>Default audit timeline (<c>DapperAuditRepository.GetByScopeAsync</c>).</summary>
    public const string AuditEventsGetByScope = """
                                                SELECT TOP (@Take)
                                                    EventId, OccurredUtc, EventType,
                                                    ActorUserId, ActorUserName,
                                                    TenantId, WorkspaceId, ProjectId,
                                                    RunId, ManifestId, ArtifactId,
                                                    DataJson, CorrelationId
                                                FROM dbo.AuditEvents
                                                WHERE TenantId = @TenantId
                                                  AND WorkspaceId = @WorkspaceId
                                                  AND ProjectId = @ProjectId
                                                ORDER BY OccurredUtc DESC, EventId DESC;
                                                """;

    /// <summary>
    ///     Opening clause for filtered audit search (<c>DapperAuditRepository.GetFilteredAsync</c>);
    ///     dynamic filters append <c>AND …</c> before <see cref="AuditEventsFilteredOrderByOccurredUtcEventIdDesc" />.
    /// </summary>
    public const string AuditEventsFilteredSelectFromWhereScope = """
                                                                    SELECT TOP (@Take)
                                                                        EventId, OccurredUtc, EventType,
                                                                        ActorUserId, ActorUserName,
                                                                        TenantId, WorkspaceId, ProjectId,
                                                                        RunId, ManifestId, ArtifactId,
                                                                        DataJson, CorrelationId
                                                                    FROM dbo.AuditEvents
                                                                    WHERE TenantId = @TenantId
                                                                      AND WorkspaceId = @WorkspaceId
                                                                      AND ProjectId = @ProjectId
                                                                    """;

    /// <summary>Stable keyset ordering for audit search/export listings.</summary>
    public const string AuditEventsFilteredOrderByOccurredUtcEventIdDesc = """
                                                                             ORDER BY OccurredUtc DESC, EventId DESC;
                                                                             """;
}
