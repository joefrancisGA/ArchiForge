using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Governance;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

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
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
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
            {SqlPagingSyntax.FirstRowsOnly(200)};
            """;

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
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
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
            {SqlPagingSyntax.FirstRowsOnly(200)};
            """;

        IEnumerable<GovernanceEnvironmentActivation> rows = await connection.QueryAsync<GovernanceEnvironmentActivation>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows];
    }
}
