using ArchiForge.Persistence.Backfill;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Backfill.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Any(static a => a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h", StringComparison.OrdinalIgnoreCase)))
        {
            PrintHelp();
            return 0;
        }

        if (!TryParseConnectionString(args, out string? connectionString) || string.IsNullOrWhiteSpace(connectionString))
        {
            await Console.Error.WriteLineAsync(
                "Set ARCHIFORGE_SQL, or pass --connection \"...\", or a positional connection string. Use --help for scope flags.");
            return 1;
        }

        SqlRelationalBackfillOptions options = ParseBackfillOptions(args);

        ServiceCollection services = new();
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddSingleton<SqlContextSnapshotRepository>();
        services.AddSingleton<SqlGraphSnapshotRepository>();
        services.AddSingleton<SqlFindingsSnapshotRepository>();
        services.AddSingleton<SqlGoldenManifestRepository>();
        services.AddSingleton<SqlArtifactBundleRepository>();
        services.AddSingleton<SqlRelationalBackfillService>();
        services.AddSingleton<ISqlRelationalBackfillService>(sp => sp.GetRequiredService<SqlRelationalBackfillService>());
        services.AddLogging(
            builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

        await using ServiceProvider provider = services.BuildServiceProvider();
        ISqlRelationalBackfillService backfill = provider.GetRequiredService<ISqlRelationalBackfillService>();

        SqlRelationalBackfillReport report = await backfill.RunAsync(options, CancellationToken.None);

        Console.WriteLine(
            $"Backfill finished. Processed={report.ProcessedCount} Success={report.SuccessCount} Failures={report.FailureCount}");

        foreach (SqlRelationalBackfillFailure failure in report.Failures)
            Console.WriteLine($"{failure.Stage} {failure.EntityKey}: {failure.Message}");

        return report.FailureCount > 0 ? 2 : 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            ArchiForge.Backfill.Cli — one-time JSON → relational backfill (no schema changes).

            Connection (first match wins):
              ARCHIFORGE_SQL environment variable
              --connection|-c "<ADO.NET connection string>"
              first positional argument (if not a flag)

            Scope (default: all stages enabled):
              --only <list>   Comma-separated: context, graph, findings, golden, artifact
                              Enables only those stages (others off).
              --skip-context|--skip-graph|--skip-findings|--skip-golden|--skip-artifact
                              Turns off that stage (combine with default all-on).

            If --only is present, --skip-* flags are ignored.

            Exit: 0 success, 1 bad usage / no connection, 2 one or more entity failures.
            """);
    }

    private static bool TryParseConnectionString(string[] args, out string? connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ARCHIFORGE_SQL");

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a.Equals("--connection", StringComparison.OrdinalIgnoreCase) || a.Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                    connectionString = args[++i];

                continue;
            }

            if (a.StartsWith('-'))
                continue;

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = a;
        }

        return !string.IsNullOrWhiteSpace(connectionString);
    }

    private static SqlRelationalBackfillOptions ParseBackfillOptions(string[] args)
    {
        string? onlyList = null;
        bool skipContext = false;
        bool skipGraph = false;
        bool skipFindings = false;
        bool skipGolden = false;
        bool skipArtifact = false;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a.Equals("--only", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                onlyList = args[++i];
                continue;
            }

            if (a.Equals("--skip-context", StringComparison.OrdinalIgnoreCase))
                skipContext = true;
            else if (a.Equals("--skip-graph", StringComparison.OrdinalIgnoreCase))
                skipGraph = true;
            else if (a.Equals("--skip-findings", StringComparison.OrdinalIgnoreCase))
                skipFindings = true;
            else if (a.Equals("--skip-golden", StringComparison.OrdinalIgnoreCase))
                skipGolden = true;
            else if (a.Equals("--skip-artifact", StringComparison.OrdinalIgnoreCase))
                skipArtifact = true;
        }

        if (string.IsNullOrWhiteSpace(onlyList))
            return new SqlRelationalBackfillOptions
            {
                ContextSnapshots = !skipContext,
                GraphSnapshots = !skipGraph,
                FindingsSnapshots = !skipFindings,
                GoldenManifestsPhase1 = !skipGolden,
                ArtifactBundles = !skipArtifact,
            };
        
        HashSet<string> stages = new(StringComparer.OrdinalIgnoreCase);
        foreach (string part in onlyList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryMapStage(part, out string key))
                stages.Add(key);
        }

        return new SqlRelationalBackfillOptions
        {
            ContextSnapshots = stages.Contains("context"),
            GraphSnapshots = stages.Contains("graph"),
            FindingsSnapshots = stages.Contains("findings"),
            GoldenManifestsPhase1 = stages.Contains("golden"),
            ArtifactBundles = stages.Contains("artifact"),
        };

    }

    private static bool TryMapStage(string token, out string key)
    {
        string? mapped = token.Trim().ToLowerInvariant() switch
        {
            "context" => "context",
            "graph" => "graph",
            "findings" => "findings",
            "golden" => "golden",
            "artifact" => "artifact",
            "artifacts" => "artifact",
            _ => null,
        };

        if (mapped is null)
        {
            key = string.Empty;
            return false;
        }

        key = mapped;
        return true;
    }
}
