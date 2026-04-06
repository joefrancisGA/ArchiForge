using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>Persists <see cref="RuleAuditTrace"/> from decisioning (not API <c>DecisionTraces</c> table).</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlDecisionTraceRepository(ISqlConnectionFactory connectionFactory) : IDecisionTraceRepository
{
    public async Task SaveAsync(
        RuleAuditTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(trace);

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

        object args = new
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

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<RuleAuditTrace?> GetByIdAsync(ScopeContext scope, Guid decisionTraceId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        DecisionTraceRow? row = await connection.QuerySingleOrDefaultAsync<DecisionTraceRow>(
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

        return new RuleAuditTrace
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
        public Guid TenantId { get; init; }
        public Guid WorkspaceId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid DecisionTraceId { get; init; }
        public Guid RunId { get; init; }
        public DateTime CreatedUtc { get; init; }
        public string RuleSetId { get; init; } = null!;
        public string RuleSetVersion { get; init; } = null!;
        public string RuleSetHash { get; init; } = null!;
        public string AppliedRuleIdsJson { get; init; } = null!;
        public string AcceptedFindingIdsJson { get; init; } = null!;
        public string RejectedFindingIdsJson { get; init; } = null!;
        public string NotesJson { get; init; } = null!;
    }
}
