using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class AgentEvidencePackageRepository(IDbConnectionFactory connectionFactory)
    : IAgentEvidencePackageRepository
{
    public async Task CreateAsync(
        AgentEvidencePackage evidencePackage,
        CancellationToken cancellationToken = default)
    {
        // Delete any existing package for this run before inserting so that a retry
        // (e.g. after a partial failure in ExecuteRunAsync) does not accumulate stale rows.
        const string deleteSql = """
            DELETE FROM AgentEvidencePackages WHERE RunId = @RunId;
            """;

        const string insertSql = """
            INSERT INTO AgentEvidencePackages
            (
                EvidencePackageId,
                RunId,
                RequestId,
                SystemName,
                Environment,
                CloudProvider,
                EvidenceJson,
                CreatedUtc
            )
            VALUES
            (
                @EvidencePackageId,
                @RunId,
                @RequestId,
                @SystemName,
                @Environment,
                @CloudProvider,
                @EvidenceJson,
                @CreatedUtc
            );
            """;

        var json = JsonSerializer.Serialize(evidencePackage, ContractJson.Default);

        using var connection = connectionFactory.CreateConnection();

        var parameters = new
        {
            evidencePackage.EvidencePackageId,
            evidencePackage.RunId,
            evidencePackage.RequestId,
            evidencePackage.SystemName,
            evidencePackage.Environment,
            evidencePackage.CloudProvider,
            EvidenceJson = json,
            evidencePackage.CreatedUtc
        };

        await connection.ExecuteAsync(new CommandDefinition(
            deleteSql,
            new { evidencePackage.RunId },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            parameters,
            cancellationToken: cancellationToken));
    }

    public async Task<AgentEvidencePackage?> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1 EvidenceJson
            FROM AgentEvidencePackages
            WHERE RunId = @RunId
            ORDER BY CreatedUtc DESC;
            """;

        using var connection = connectionFactory.CreateConnection();

        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<AgentEvidencePackage>(json, ContractJson.Default);
    }

    public async Task<AgentEvidencePackage?> GetByIdAsync(
        string evidencePackageId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EvidenceJson
            FROM AgentEvidencePackages
            WHERE EvidencePackageId = @EvidencePackageId;
            """;

        using var connection = connectionFactory.CreateConnection();

        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                EvidencePackageId = evidencePackageId
            },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<AgentEvidencePackage>(json, ContractJson.Default);
    }
}
