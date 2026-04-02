using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class AgentEvidencePackageRepository(IDbConnectionFactory connectionFactory)
    : IAgentEvidencePackageRepository
{
    public async Task CreateAsync(
        AgentEvidencePackage evidencePackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidencePackage);

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

        string json = JsonSerializer.Serialize(evidencePackage, ContractJson.Default);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

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

        using IDbTransaction tx = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            deleteSql,
            new { evidencePackage.RunId },
            transaction: tx,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            parameters,
            transaction: tx,
            cancellationToken: cancellationToken));

        tx.Commit();
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RunId = runId
            },
            cancellationToken: cancellationToken));

        return DeserializePackage(json, $"run '{runId}'");
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                EvidencePackageId = evidencePackageId
            },
            cancellationToken: cancellationToken));

        return DeserializePackage(json, $"package '{evidencePackageId}'");
    }

    private static AgentEvidencePackage? DeserializePackage(string? json, string context)
    {
        if (json is null)
            return null;

        AgentEvidencePackage? package;
        try
        {
            package = JsonSerializer.Deserialize<AgentEvidencePackage>(json, ContractJson.Default);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Evidence package JSON for {context} could not be deserialized. " +
                "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }

        return package
            ?? throw new InvalidOperationException(
                $"Evidence package JSON for {context} deserialized to null. " +
                "The stored JSON may be empty or corrupt.");
    }
}
