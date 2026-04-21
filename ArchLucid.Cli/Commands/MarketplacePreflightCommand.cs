using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary>Entry point for <c>archlucid marketplace preflight</c> — repo-local checks only (no live keys).</summary>
[ExcludeFromCodeCoverage(Justification = "Console I/O entry; behaviour covered by MarketplacePreflightRunnerTests + command tests.")]
internal static class MarketplacePreflightCommand
{
    private static readonly JsonSerializerOptions JsonCamel =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

    public static Task<int> RunAsync(string[] args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        string? repoOverride = null;

        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i];

            if (string.Equals(token, "--repo", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    WriteUsage("Missing value for --repo.");

                    return Task.FromResult(CliExitCode.UsageError);
                }

                repoOverride = args[++i].Trim();

                continue;
            }

            WriteUsage($"Unexpected argument: {token}");

            return Task.FromResult(CliExitCode.UsageError);
        }

        string? root = string.IsNullOrWhiteSpace(repoOverride)
            ? CliRepositoryRootResolver.TryResolveRepositoryRoot()
            : Path.GetFullPath(repoOverride);

        if (root is null || !Directory.Exists(root))
        {
            Console.Error.WriteLine(
                "Could not locate repository root (expected docs/go-to-market/MARKETPLACE_PUBLICATION.md). " +
                "Run from the repo tree or pass --repo <absolute-or-relative-path>.");

            return Task.FromResult(CliExitCode.UsageError);
        }

        IReadOnlyList<MarketplacePreflightStepResult> steps = MarketplacePreflightRunner.Evaluate(root);
        bool allPassed = steps.All(static s => s.Passed);

        if (CliExecutionContext.JsonOutput)
        {
            object payload = new
            {
                repositoryRoot = root,
                allPassed,
                steps = steps.Select(static s => new { id = s.Id, passed = s.Passed, detail = s.Detail }).ToList(),
            };

            Console.WriteLine(JsonSerializer.Serialize(payload, JsonCamel));

            return Task.FromResult(allPassed ? CliExitCode.Success : CliExitCode.OperationFailed);
        }

        Console.WriteLine($"archlucid marketplace preflight @ {root}");
        Console.WriteLine(new string('-', 72));
        Console.WriteLine(
            "Automated checks only — Partner Center seller verification, tax, payout, and \"Go live\" are owner steps.");
        Console.WriteLine(new string('-', 72));

        foreach (MarketplacePreflightStepResult step in steps)
        {
            string verdict = step.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{verdict}] {step.Id,-40} {step.Detail}");
        }

        Console.WriteLine(new string('-', 72));
        Console.WriteLine(
            allPassed
                ? "PASS — see docs/go-to-market/MARKETPLACE_PUBLICATION.md for human checklist items."
                : "FAIL — fix repository/doc drift before publishing in Partner Center.");

        return Task.FromResult(allPassed ? CliExitCode.Success : CliExitCode.OperationFailed);
    }

    private static void WriteUsage(string reason)
    {
        Console.Error.WriteLine(reason);
        Console.Error.WriteLine("Usage: archlucid marketplace preflight [--repo <dir>]");
    }
}
