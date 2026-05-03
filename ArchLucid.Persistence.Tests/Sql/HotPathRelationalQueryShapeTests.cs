using ArchLucid.Persistence.Sql;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Sql;

/// <summary>
///     Guards SQL text for high-volume run/audit list paths â€” deterministic string assertions only (no DB).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class HotPathRelationalQueryShapeTests
{
    [SkippableFact]
    public void Runs_list_by_project_retains_nolock_scope_archived_filter_and_created_order()
    {
        string sql = HotPathRelationalQueryShapes.RunsListByProjectNoLock;

        sql.Should().Contain("SELECT TOP (@Take)");
        sql.Should().Contain("FROM dbo.Runs WITH (NOLOCK)");
        sql.Should().Contain("ProjectId = @ProjectSlug");
        sql.Should().Contain("TenantId = @TenantId");
        sql.Should().Contain("WorkspaceId = @WorkspaceId");
        sql.Should().Contain("ScopeProjectId = @ScopeProjectId");
        sql.Should().Contain("ArchivedUtc IS NULL");
        sql.Should().Contain("ORDER BY CreatedUtc DESC");
    }

    [SkippableFact]
    public void Runs_list_by_project_keyset_retains_cursor_predicate_and_run_id_tie_break()
    {
        string sql = HotPathRelationalQueryShapes.RunsListByProjectKeysetNoLock;

        sql.Should().Contain("FROM dbo.Runs WITH (NOLOCK)");
        sql.Should().Contain("SELECT TOP (@Fetch)");
        sql.Should().Contain("@CursorRunId");
        sql.Should().Contain("@CursorCreatedUtc");
        sql.Should().Contain("ArchivedUtc IS NULL");
        sql.Should().Contain("ORDER BY CreatedUtc DESC, RunId DESC");
    }

    [SkippableFact]
    public void Committed_architecture_review_exists_retains_scope_join_and_commit_predicate()
    {
        string sql = HotPathRelationalQueryShapes.CommittedArchitectureReviewExistsNoLock;

        sql.Should().Contain("CASE WHEN EXISTS");
        sql.Should().Contain("FROM dbo.Runs r WITH (NOLOCK)");
        sql.Should().Contain("dbo.GoldenManifests gm WITH (NOLOCK)");
        sql.Should().Contain("TenantId = @TenantId");
        sql.Should().Contain("WorkspaceId = @WorkspaceId");
        sql.Should().Contain("ScopeProjectId = @ScopeProjectId");
        sql.Should().Contain("LegacyRunStatus = @CommittedStatus");
        sql.Should().Contain("GoldenManifestId IS NOT NULL");
    }

    [SkippableFact]
    public void Runs_list_recent_in_scope_retains_nolock_scope_archived_filter_and_created_order()
    {
        string sql = HotPathRelationalQueryShapes.RunsListRecentInScopeNoLock;

        sql.Should().Contain("SELECT TOP (@Take)");
        sql.Should().Contain("FROM dbo.Runs WITH (NOLOCK)");
        sql.Should().Contain("TenantId = @TenantId");
        sql.Should().Contain("WorkspaceId = @WorkspaceId");
        sql.Should().Contain("ScopeProjectId = @ScopeProjectId");
        sql.Should().Contain("ArchivedUtc IS NULL");
        sql.Should().Contain("ORDER BY CreatedUtc DESC");
    }

    [SkippableFact]
    public void Runs_list_recent_in_scope_keyset_matches_project_keyset_cursor_pattern()
    {
        string sql = HotPathRelationalQueryShapes.RunsListRecentInScopeKeysetNoLock;

        sql.Should().Contain("FROM dbo.Runs WITH (NOLOCK)");
        sql.Should().Contain("SELECT TOP (@Fetch)");
        sql.Should().Contain("@CursorRunId");
        sql.Should().Contain("ORDER BY CreatedUtc DESC, RunId DESC");
        sql.Should().NotContain("ProjectId = @ProjectSlug");
    }

    [SkippableFact]
    public void Audit_get_by_scope_retains_scope_and_stable_occurred_event_order()
    {
        string sql = HotPathRelationalQueryShapes.AuditEventsGetByScope;

        sql.Should().Contain("FROM dbo.AuditEvents");
        sql.Should().Contain("SELECT TOP (@Take)");
        sql.Should().Contain("TenantId = @TenantId");
        sql.Should().Contain("WorkspaceId = @WorkspaceId");
        sql.Should().Contain("ProjectId = @ProjectId");
        sql.Should().Contain("ORDER BY OccurredUtc DESC, EventId DESC");
    }

    [SkippableFact]
    public void Audit_filtered_shape_prefix_suffix_allow_dynamic_and_predicate_between()
    {
        string prefix = HotPathRelationalQueryShapes.AuditEventsFilteredSelectFromWhereScope;
        string suffix = HotPathRelationalQueryShapes.AuditEventsFilteredOrderByOccurredUtcEventIdDesc;

        prefix.Should().Contain("FROM dbo.AuditEvents");
        prefix.Should().Contain("TenantId = @TenantId");
        prefix.Should().Contain("AND ProjectId = @ProjectId");

        suffix.Should().Contain("ORDER BY OccurredUtc DESC, EventId DESC");

        string combined = $"{prefix}\n{suffix.Trim()}";

        combined.Should().MatchRegex(@"(?s)@ProjectId\s+ORDER BY OccurredUtc DESC, EventId DESC");
    }
}
