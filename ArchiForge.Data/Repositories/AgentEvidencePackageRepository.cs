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
        const string sql = """
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

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                evidencePackage.EvidencePackageId,
                evidencePackage.RunId,
                evidencePackage.RequestId,
                evidencePackage.SystemName,
                evidencePackage.Environment,
                evidencePackage.CloudProvider,
                EvidenceJson = json,
                evidencePackage.CreatedUtc
            },
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
            new { RunId = runId },
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
            new { EvidencePackageId = evidencePackageId },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<AgentEvidencePackage>(json, ContractJson.Default);
    }
}
