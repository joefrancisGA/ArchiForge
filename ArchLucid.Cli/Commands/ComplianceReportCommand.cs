using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Generates a Markdown compliance mapping report from <c>docs/security/SOC2_SELF_ASSESSMENT_2026.md</c> plus local
///     configuration, validate-config findings, optional live audit sampling, and an informal SOC 2 ↔ ISO 27001 table.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "CLI orchestration; covered by ComplianceReportCommandTests and unit tests.")]
internal static class ComplianceReportCommand
{
    internal static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ComplianceReportOptions? options = ComplianceReportOptions.Parse(args, out string? parseError);

        if (options is null)
        {
            await Console.Error.WriteLineAsync(parseError);
            await Console.Error.WriteLineAsync(
                "Usage: archlucid compliance-report [--out <file.md>] [--repo <dir>] [--with-live-audit]");

            return CliExitCode.UsageError;
        }

        string cwd = Directory.GetCurrentDirectory();

        if (!ComplianceReportRepositoryRootResolver.TryResolve(options.RepoRoot, cwd, out string? repositoryRoot))
        {
            await Console.Error.WriteLineAsync(
                "Could not locate SOC 2 template. Expected file: docs/security/SOC2_SELF_ASSESSMENT_2026.md " +
                "(pass --repo <path-to-archlucid-repository> or run from inside the repository tree).");

            return CliExitCode.ConfigurationError;
        }

        string templatePath = Path.Combine(repositoryRoot, ComplianceReportRepositoryRootResolver.Soc2TemplateRelativePath);

        string templateBody;

        try
        {
            templateBody = await File.ReadAllTextAsync(templatePath, Encoding.UTF8, cancellationToken);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Failed to read template: {ex.Message}");

            return CliExitCode.OperationFailed;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? cli = CliCommandShared.TryLoadConfigFromCwd();
        string contentRoot = cwd;
        bool appsettingsExists = ValidateConfigConfigurationFactory.AppsettingsFileExists(contentRoot);
        IConfiguration configuration = ValidateConfigConfigurationFactory.BuildMerged(cli);

        IReadOnlyList<ValidateConfigFinding> findings = ValidateConfigEvaluator.Evaluate(
            configuration,
            contentRoot,
            appsettingsExists);

        string configurationTable = ComplianceReportConfigurationSnapshotFormatter.Build(configuration, contentRoot, appsettingsExists);
        string validateSummary = ComplianceReportValidateConfigSummaryFormatter.Build(findings);

        ComplianceReportAuditLiveSample? liveSample = null;

        if (options.WithLiveAudit)
        {
            string baseUrl = CliCommandShared.GetBaseUrl(cli);
            ApiConnectionOutcome outcome = await CliCommandShared.TryConnectToApiAsync(baseUrl, cli, cancellationToken);

            if (outcome == ApiConnectionOutcome.Connected)
            {
                using HttpClient http = ArchLucidApiClient.CreateSharedApiHttpClient(baseUrl, cli);
                liveSample = await ComplianceReportAuditLiveSampleFetcher.TryFetchAsync(http, cancellationToken);
            }
            else
            {
                liveSample = new ComplianceReportAuditLiveSample(
                    false,
                    "API health check failed — skipped live audit page.",
                    0,
                    new Dictionary<string, int>(),
                    null,
                    null);
            }
        }

        string markdown = ComplianceReportMarkdownComposer.Compose(
            templateBody,
            repositoryRoot,
            DateTime.UtcNow.ToString("O"),
            Environment.MachineName,
            cwd,
            configurationTable,
            validateSummary,
            liveSample,
            options.WithLiveAudit);

        if (string.IsNullOrWhiteSpace(options.OutPath))
        {
            Console.WriteLine(markdown);
        }
        else
        {
            await File.WriteAllTextAsync(options.OutPath, markdown, Encoding.UTF8, cancellationToken);
            Console.WriteLine($"Wrote compliance report to {options.OutPath}");
        }

        return CliExitCode.Success;
    }
}
