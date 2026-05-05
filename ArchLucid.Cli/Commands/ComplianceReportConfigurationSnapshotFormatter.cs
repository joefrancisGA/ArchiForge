using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Commands;

/// <summary>Redacted snapshot of merged host configuration for compliance narratives.</summary>
internal static class ComplianceReportConfigurationSnapshotFormatter
{
    internal static string Build(
        IConfiguration configuration,
        string contentRoot,
        bool appsettingsExists)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(contentRoot);

        List<string> lines =
        [
            "| Key | Value |",
            "|-----|-------|",
            Row("Content root", contentRoot),
            Row("`appsettings.json` present", appsettingsExists ? "yes" : "no"),
            Row("ASPNETCORE_ENVIRONMENT", OrPlaceholder(configuration["ASPNETCORE_ENVIRONMENT"])),
            Row("DOTNET_ENVIRONMENT", OrPlaceholder(configuration["DOTNET_ENVIRONMENT"])),
            Row("ArchLucid:StorageProvider", OrPlaceholder(configuration["ArchLucid:StorageProvider"])),
            Row("ArchLucidAuth:Mode", OrPlaceholder(configuration["ArchLucidAuth:Mode"])),
            Row("AgentExecution:Mode", OrPlaceholder(configuration["AgentExecution:Mode"])),
            Row(
                "ConnectionStrings:ArchLucid",
                string.IsNullOrWhiteSpace(configuration["ConnectionStrings:ArchLucid"]) ? "(absent)" : "(present — redacted)"),
            Row("Authentication:ApiKey:Enabled", OrPlaceholder(configuration["Authentication:ApiKey:Enabled"])),
            Row("ARCHLUCID_API_URL (env)", OrPlaceholder(Environment.GetEnvironmentVariable("ARCHLUCID_API_URL"))),
        ];

        return string.Join(Environment.NewLine, lines);
    }

    private static string Row(string k, string v)
    {
        return $"| {EscapePipe(k)} | {EscapePipe(v)} |";
    }

    private static string EscapePipe(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string OrPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(not set)";

        return value.Trim();
    }
}
