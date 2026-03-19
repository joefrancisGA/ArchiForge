using System.Text.Json;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class EvidenceBundleRepository(IDbConnectionFactory connectionFactory) : IEvidenceBundleRepository
{
    public async Task CreateAsync(EvidenceBundle evidenceBundle, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO EvidenceBundles
            (
                EvidenceBundleId,
                RequestDescription,
                EvidenceJson,
                CreatedUtc
            )
            VALUES
            (
                @EvidenceBundleId,
                @RequestDescription,
                @EvidenceJson,
                @CreatedUtc
            );
            """;

        var json = JsonSerializer.Serialize(evidenceBundle, ContractJson.Default);

        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                evidenceBundle.EvidenceBundleId,
                evidenceBundle.RequestDescription,
                EvidenceJson = json,
                CreatedUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    public async Task<EvidenceBundle?> GetByIdAsync(string evidenceBundleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EvidenceJson
            FROM EvidenceBundles
            WHERE EvidenceBundleId = @EvidenceBundleId;
            """;

        using var connection = connectionFactory.CreateConnection();

        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new { EvidenceBundleId = evidenceBundleId },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<EvidenceBundle>(json, ContractJson.Default);
    }
}