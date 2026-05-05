using System.Diagnostics.CodeAnalysis;

using ArchLucid.Persistence.Data.Infrastructure;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.MigrateVerify;

/// <summary>CI/dev entrypoint that applies embedded DbUp scripts via <see cref="DatabaseMigrator.Run" />.</summary>
/// <remarks>
///     Intended for pipelines that provision an empty SQL catalog then verify migrations alone (no HTTP host).
/// </remarks>
[ExcludeFromCodeCoverage(Justification =
    "Thin CI entrypoint; Tier 1.5 GitHub Actions job exercises migrations end-to-end against Docker SQL Server.")]
internal static class Program
{
    /// <summary>Env var consumed by Tier 1.5 CI (must include Initial Catalog).</summary>
    internal const string ConnectionStringEnvironmentVariableName = "ARCHLUCID_CI_DBUP_CONNECTION_STRING";

    private static async Task<int> Main(string[] args)
    {
        if (!TryReadConnectionString(args, out string connectionString, out string usageError))
        {
            await Console.Error.WriteLineAsync(usageError);

            return 1;
        }

        try
        {
            DatabaseMigrator.Run(connectionString);
            Console.WriteLine("DbUp migrations applied successfully.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DbUp failed: {ex.Message}");
            Console.Error.WriteLine(ex);

            return 2;
        }
    }

    /// <summary>Resolves connection string from env (CI) or first CLI arg (local).</summary>
    internal static bool TryReadConnectionString(
        IReadOnlyList<string>? args,
        out string connectionString,
        out string usageError)
    {
        string? raw = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariableName);

        if (string.IsNullOrWhiteSpace(raw) && args is { Count: > 0 })
            raw = args[0];

        if (string.IsNullOrWhiteSpace(raw))
        {
            connectionString = string.Empty;

            usageError =
                $"{ConnectionStringEnvironmentVariableName} is unset and no connection string argument was provided. "
                + $"Set env or pass SQL connection string as the first argument.";


            return false;
        }

        SqlConnectionStringBuilder builder = new(raw.Trim());

        // Treat empty/null catalog as programmer error early (DatabaseMigrator would target master otherwise).
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            connectionString = string.Empty;

            usageError = "Initial Catalog is required.";

            return false;
        }

        connectionString = raw.Trim();
        usageError = string.Empty;

        return true;
    }
}
