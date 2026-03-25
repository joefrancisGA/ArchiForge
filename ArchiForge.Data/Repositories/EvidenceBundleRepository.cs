using System.Data;
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
        ArgumentNullException.ThrowIfNull(evidenceBundle);
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

        string json = JsonSerializer.Serialize(evidenceBundle, ContractJson.Default);

        using IDbConnection connection = connectionFactory.CreateConnection();

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

        using IDbConnection connection = connectionFactory.CreateConnection();

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                EvidenceBundleId = evidenceBundleId
            },
            cancellationToken: cancellationToken));

        if (json is null)
            return null;

        EvidenceBundle? bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<EvidenceBundle>(json, ContractJson.Default);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Evidence bundle JSON for '{evidenceBundleId}' could not be deserialized. " +
                "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }

        return bundle
            ?? throw new InvalidOperationException(
                $"Evidence bundle JSON for '{evidenceBundleId}' deserialized to null. " +
                "The stored JSON may be empty or corrupt.");
    }
}
