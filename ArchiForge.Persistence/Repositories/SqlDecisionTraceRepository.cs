using System.Data;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;
using Dapper;

namespace ArchiForge.Persistence.Repositories;

/// <summary>Persists <see cref="DecisionTrace"/> from decisioning (not API <c>DecisionTraces</c> table).</summary>
public sealed class SqlDecisionTraceRepository : IDecisionTraceRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlDecisionTraceRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        DecisionTrace trace,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO dbo.DecisioningTraces
            (
                DecisionTraceId, RunId, CreatedUtc,
                RuleSetId, RuleSetVersion, RuleSetHash,
                AppliedRuleIdsJson, AcceptedFindingIdsJson, RejectedFindingIdsJson, NotesJson
            )
            VALUES
            (
                @DecisionTraceId, @RunId, @CreatedUtc,
                @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @AppliedRuleIdsJson, @AcceptedFindingIdsJson, @RejectedFindingIdsJson, @NotesJson
            );
            """;

        var args = new
        {
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

        await using var owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await owned.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    public async Task<DecisionTrace?> GetByIdAsync(Guid decisionTraceId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                DecisionTraceId, RunId, CreatedUtc,
                RuleSetId, RuleSetVersion, RuleSetHash,
                AppliedRuleIdsJson, AcceptedFindingIdsJson, RejectedFindingIdsJson, NotesJson
            FROM dbo.DecisioningTraces
            WHERE DecisionTraceId = @DecisionTraceId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<DecisionTraceRow>(
            new CommandDefinition(sql, new { DecisionTraceId = decisionTraceId }, cancellationToken: ct));

        if (row is null)
            return null;

        return new DecisionTrace
        {
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
        public Guid DecisionTraceId { get; set; }
        public Guid RunId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string RuleSetId { get; set; } = default!;
        public string RuleSetVersion { get; set; } = default!;
        public string RuleSetHash { get; set; } = default!;
        public string AppliedRuleIdsJson { get; set; } = default!;
        public string AcceptedFindingIdsJson { get; set; } = default!;
        public string RejectedFindingIdsJson { get; set; } = default!;
        public string NotesJson { get; set; } = default!;
    }
}
