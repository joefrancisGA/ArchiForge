using ArchLucid.ContextIngestion.Models;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

using ArchLucid.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>
/// Seeds the minimal Runs / ContextSnapshots / GraphSnapshots / FindingsSnapshots / DecisioningTraces chain
/// required before persisting a <see cref="ArchLucid.Decisioning.Models.GoldenManifest"/> under FK constraints.
/// </summary>
public static class AuthorityRunChainTestSeed
{
    /// <summary>Inserts <c>dbo.Runs</c> only (FK target for <c>DecisioningTraces.RunId</c>).</summary>
    public static async Task InsertRunAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        string projectSlug,
        CancellationToken ct)
    {
        const string insertRun = """
            INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
            VALUES (@RunId, @ProjectId, @CreatedUtc, @TenantId, @WorkspaceId, @ScopeProjectId);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRun,
                new
                {
                    RunId = runId,
                    ProjectId = projectSlug,
                    CreatedUtc = DateTime.UtcNow,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ScopeProjectId = projectId,
                },
                cancellationToken: ct));
    }

    /// <summary>Inserts <c>dbo.Runs</c> and <c>dbo.ContextSnapshots</c> only (for tests that insert <c>dbo.GraphSnapshots</c> headers directly).</summary>
    public static async Task SeedRunAndContextOnlyAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        Guid contextSnapshotId,
        string projectSlug,
        CancellationToken ct)
    {
        await InsertRunAsync(connection, tenantId, workspaceId, projectId, runId, projectSlug, ct);

        string emptyCanonical = JsonEntitySerializer.Serialize(new List<CanonicalObject>());
        string emptyList = JsonEntitySerializer.Serialize(new List<string>());

        const string insertContext = """
            INSERT INTO dbo.ContextSnapshots
            (
                SnapshotId, RunId, ProjectId, CreatedUtc,
                CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
            )
            VALUES
            (
                @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertContext,
                new
                {
                    SnapshotId = contextSnapshotId,
                    RunId = runId,
                    ProjectId = projectSlug,
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = emptyCanonical,
                    DeltaSummary = (string?)null,
                    WarningsJson = emptyList,
                    ErrorsJson = emptyList,
                    SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>()),
                },
                cancellationToken: ct));
    }

    /// <inheritdoc cref="AuthorityRunChainTestSeed"/>
    public static async Task SeedFullChainAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        Guid contextSnapshotId,
        Guid graphSnapshotId,
        Guid findingsSnapshotId,
        Guid decisionTraceId,
        string projectSlug,
        CancellationToken ct)
    {
        await InsertRunAsync(connection, tenantId, workspaceId, projectId, runId, projectSlug, ct);

        string emptyCanonical = JsonEntitySerializer.Serialize(new List<CanonicalObject>());
        string emptyList = JsonEntitySerializer.Serialize(new List<string>());

        const string insertContext = """
            INSERT INTO dbo.ContextSnapshots
            (
                SnapshotId, RunId, ProjectId, CreatedUtc,
                CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
            )
            VALUES
            (
                @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertContext,
                new
                {
                    SnapshotId = contextSnapshotId,
                    RunId = runId,
                    ProjectId = projectSlug,
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = emptyCanonical,
                    DeltaSummary = (string?)null,
                    WarningsJson = emptyList,
                    ErrorsJson = emptyList,
                    SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>()),
                },
                cancellationToken: ct));

        string emptyNodes = JsonEntitySerializer.Serialize(new List<GraphNode>());
        string emptyEdges = JsonEntitySerializer.Serialize(new List<GraphEdge>());
        string emptyGraphWarnings = JsonEntitySerializer.Serialize(new List<string>());

        const string insertGraph = """
            INSERT INTO dbo.GraphSnapshots
            (
                GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                NodesJson, EdgesJson, WarningsJson
            )
            VALUES
            (
                @GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc,
                @NodesJson, @EdgesJson, @WarningsJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertGraph,
                new
                {
                    GraphSnapshotId = graphSnapshotId,
                    ContextSnapshotId = contextSnapshotId,
                    RunId = runId,
                    CreatedUtc = DateTime.UtcNow,
                    NodesJson = emptyNodes,
                    EdgesJson = emptyEdges,
                    WarningsJson = emptyGraphWarnings,
                },
                cancellationToken: ct));

        const string insertFindings = """
            INSERT INTO dbo.FindingsSnapshots
            (
                FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                SchemaVersion, FindingsJson
            )
            VALUES
            (
                @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                @SchemaVersion, @FindingsJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertFindings,
                new
                {
                    FindingsSnapshotId = findingsSnapshotId,
                    RunId = runId,
                    ContextSnapshotId = contextSnapshotId,
                    GraphSnapshotId = graphSnapshotId,
                    CreatedUtc = DateTime.UtcNow,
                    SchemaVersion = 1,
                    FindingsJson = JsonEntitySerializer.Serialize(new FindingsSnapshot
                    {
                        FindingsSnapshotId = findingsSnapshotId,
                        RunId = runId,
                        ContextSnapshotId = contextSnapshotId,
                        GraphSnapshotId = graphSnapshotId,
                        CreatedUtc = DateTime.UtcNow,
                        Findings = [],
                    }),
                },
                cancellationToken: ct));

        const string insertTrace = """
            INSERT INTO dbo.DecisioningTraces
            (
                DecisionTraceId, RunId, CreatedUtc,
                RuleSetId, RuleSetVersion, RuleSetHash,
                AppliedRuleIdsJson, AcceptedFindingIdsJson, RejectedFindingIdsJson, NotesJson,
                TenantId, WorkspaceId, ProjectId
            )
            VALUES
            (
                @DecisionTraceId, @RunId, @CreatedUtc,
                @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @AppliedRuleIdsJson, @AcceptedFindingIdsJson, @RejectedFindingIdsJson, @NotesJson,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTrace,
                new
                {
                    DecisionTraceId = decisionTraceId,
                    RunId = runId,
                    CreatedUtc = DateTime.UtcNow,
                    RuleSetId = "rs",
                    RuleSetVersion = "1",
                    RuleSetHash = "h",
                    AppliedRuleIdsJson = emptyList,
                    AcceptedFindingIdsJson = emptyList,
                    RejectedFindingIdsJson = emptyList,
                    NotesJson = emptyList,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                },
                cancellationToken: ct));
    }
}
