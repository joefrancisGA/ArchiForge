using System.Data;

using ArchiForge.Contracts.Governance;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class GovernanceEnvironmentActivationRepository(IDbConnectionFactory connectionFactory)
    : IGovernanceEnvironmentActivationRepository
{
    public async Task CreateAsync(GovernanceEnvironmentActivation item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        const string sql = """
            INSERT INTO GovernanceEnvironmentActivations
            (
                ActivationId,
                RunId,
                ManifestVersion,
                Environment,
                IsActive,
                ActivatedUtc
            )
            VALUES
            (
                @ActivationId,
                @RunId,
                @ManifestVersion,
                @Environment,
                @IsActive,
                @ActivatedUtc
            );
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.ActivationId,
                item.RunId,
                item.ManifestVersion,
                item.Environment,
                item.IsActive,
                item.ActivatedUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(GovernanceEnvironmentActivation item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        const string sql = """
            UPDATE GovernanceEnvironmentActivations
            SET IsActive = @IsActive
            WHERE ActivationId = @ActivationId;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.ActivationId,
                item.IsActive
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByEnvironmentAsync(
        string environment,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ActivationId,
                RunId,
                ManifestVersion,
                Environment,
                IsActive,
                ActivatedUtc
            FROM GovernanceEnvironmentActivations
            WHERE Environment = @Environment
            ORDER BY ActivatedUtc DESC
            LIMIT 200;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        IEnumerable<GovernanceEnvironmentActivation> rows = await connection.QueryAsync<GovernanceEnvironmentActivation>(new CommandDefinition(
            sql,
            new { Environment = environment },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ActivationId,
                RunId,
                ManifestVersion,
                Environment,
                IsActive,
                ActivatedUtc
            FROM GovernanceEnvironmentActivations
            WHERE RunId = @RunId
            ORDER BY ActivatedUtc DESC
            LIMIT 200;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        IEnumerable<GovernanceEnvironmentActivation> rows = await connection.QueryAsync<GovernanceEnvironmentActivation>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows];
    }
}
