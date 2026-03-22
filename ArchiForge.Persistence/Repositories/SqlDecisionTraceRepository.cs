using System.Data;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

/// <summary>Persists <see cref="DecisionTrace"/> from decisioning (not API <c>DecisionTraces</c> table).</summary>
public sealed class SqlDecisionTraceRepository(ISqlConnectionFactory connectionFactory) : IDecisionTraceRepository
{
    public async Task SaveAsync(
        DecisionTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.DecisioningTraces
            (
                TenantId, WorkspaceId, ProjectId,
                DecisionTraceId, RunId, CreatedUtc,
                RuleSetId, RuleSetVersion, RuleSetHash,
                AppliedRuleIdsJson, AcceptedFindingIdsJson, RejectedFindingIdsJson, NotesJson
            )
            VALUES
            (
                @TenantId, @WorkspaceId, @ProjectId,
                @DecisionTraceId, @RunId, @CreatedUtc,
                @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @AppliedRuleIdsJson, @AcceptedFindingIdsJson, @RejectedFindingIdsJson, @NotesJson
            );
            """;

        var args = new
        {
            trace.TenantId,
            trace.WorkspaceId,
            trace.ProjectId,
            trace.DecisionTraceId,
            trace.RunId,
            trace.CreatedUtc,
            trace.RuleSetId,
            trace.RuleSetVersion,
            trace.RuleSetHash,
            AppliedRuleIdsJson = JsonEntitySerializer.Serialize(trace.AppliedRuleIds),
            AcceptedFindingIdsJson = JsonEntitySerializer.Serialize(trace.AcceptedFindingIds),
            RejectedFindingIdsJson = JsonEntitySerializer.Serialize(trace.RejectedFindingIds),
            NotesJson = JsonEntitySerializer.Serialize(trace.Notes)
        };

        if (connection is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));
            return;
        }

        await using var owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<DecisionTrace?> GetByIdAsync(ScopeContext scope, Guid decisionTraceId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                TenantId, WorkspaceId, ProjectId,
                DecisionTraceId, RunId, CreatedUtc,
                RuleSetId, RuleSetVersion, RuleSetHash,
                AppliedRuleIdsJson, AcceptedFindingIdsJson, RejectedFindingIdsJson, NotesJson
            FROM dbo.DecisioningTraces
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ScopeProjectId
              AND DecisionTraceId = @DecisionTraceId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<DecisionTraceRow>(
            new CommandDefinition(
                sql,
                new
                {
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    DecisionTraceId = decisionTraceId
                },
                cancellationToken: ct));

        if (row is null)
            return null;

        return new DecisionTrace
        {
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            DecisionTraceId = row.DecisionTraceId,
            RunId = row.RunId,
            CreatedUtc = row.CreatedUtc,
            RuleSetId = row.RuleSetId,
            RuleSetVersion = row.RuleSetVersion,
            RuleSetHash = row.RuleSetHash,
            AppliedRuleIds = JsonEntitySerializer.Deserialize<List<string>>(row.AppliedRuleIdsJson),
            AcceptedFindingIds = JsonEntitySerializer.Deserialize<List<string>>(row.AcceptedFindingIdsJson),
            RejectedFindingIds = JsonEntitySerializer.Deserialize<List<string>>(row.RejectedFindingIdsJson),
            Notes = JsonEntitySerializer.Deserialize<List<string>>(row.NotesJson)
        };
    }

    private sealed class DecisionTraceRow
    {
        public Guid TenantId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid DecisionTraceId { get; set; }
        public Guid RunId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string RuleSetId { get; set; } = null!;
        public string RuleSetVersion { get; set; } = null!;
        public string RuleSetHash { get; set; } = null!;
        public string AppliedRuleIdsJson { get; set; } = null!;
        public string AcceptedFindingIdsJson { get; set; } = null!;
        public string RejectedFindingIdsJson { get; set; } = null!;
        public string NotesJson { get; set; } = null!;
    }
}
