using System.Globalization;
using System.Text;

namespace ArchLucid.Cli.Commands;

/// <summary>Builds the final Markdown by stitching the SOC 2 template with generated SOC 2 / ISO 27001 mapping sections.</summary>
internal static class ComplianceReportMarkdownComposer
{
    internal static string Compose(
        string templateBody,
        string repositoryRoot,
        string generatedUtcIso,
        string machineName,
        string workingDirectory,
        string configurationTableMarkdown,
        string validateConfigSummaryMarkdown,
        ComplianceReportAuditLiveSample? liveAudit,
        bool liveAuditAttempted)
    {
        ArgumentNullException.ThrowIfNull(templateBody);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        StringBuilder sb = new();

        sb.AppendLine(
            $"> **Generated:** `{generatedUtcIso}` · machine `{EscapeBackticks(machineName)}` · cwd `{EscapeBackticks(workingDirectory)}` · repository root `{EscapeBackticks(repositoryName: repositoryRoot)}` · template `docs/security/SOC2_SELF_ASSESSMENT_2026.md`");
        sb.AppendLine();
        sb.AppendLine(templateBody.TrimEnd());
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## CLI-generated control mapping (SOC 2 & ISO 27001:2022 — informal)");
        sb.AppendLine();
        sb.AppendLine(
            "This section **augments** the self-assessment template above. It is **not** a SOC 2 examination, ISO certification, or Statement of Applicability. " +
            "It maps **local configuration** and **optional live audit samples** to control themes buyers often ask about.");
        sb.AppendLine();
        sb.AppendLine("### Current configuration snapshot (merged `appsettings` + environment; secrets redacted)");
        sb.AppendLine();
        sb.AppendLine(configurationTableMarkdown);
        sb.AppendLine();
        sb.AppendLine("### `archlucid validate-config` summary (same merge rules as the API host)");
        sb.AppendLine();
        sb.AppendLine(validateConfigSummaryMarkdown);
        sb.AppendLine();
        sb.AppendLine("### Audit evidence — durable catalog (repository)");
        sb.AppendLine();
        sb.AppendLine(
            $"Typed operations → audit signals are catalogued in `{Path.Combine("docs", "library", "AUDIT_COVERAGE_MATRIX.md")}` " +
            $"(repository-relative). Durable SQL audit uses append-only `dbo.AuditEvents`; export surface is documented under **`GET /v1/audit`** and **`GET /v1/audit/export`** in `docs/library/AUDIT_RETENTION_POLICY.md`.");
        sb.AppendLine();

        AppendLiveAuditSection(sb, liveAudit, liveAuditAttempted);
        sb.AppendLine("### SOC 2 themes ↔ ISO/IEC 27001:2022 Annex A (illustrative mapping)");
        sb.AppendLine();
        sb.AppendLine("| SOC 2 theme (informal) | ISO/IEC 27001:2022 Annex A | Configuration evidence (this run) | Audit log evidence (examples) |");
        sb.AppendLine("| --- | --- | --- | --- |");

        foreach (ComplianceReportSocIsoControlMap.SocIsoMappingRow row in ComplianceReportSocIsoControlMap.Rows)
        {
            sb.AppendLine(
                $"| {EscapePipe(row.Soc2Theme)} | {EscapePipe(row.Iso27001AnnexAReferences)} | {EscapePipe(row.ConfigurationEvidenceHint)} | {EscapePipe(row.AuditLogEvidenceHint)} |");
        }

        return sb.ToString();
    }

    private static void AppendLiveAuditSection(
        StringBuilder sb,
        ComplianceReportAuditLiveSample? liveAudit,
        bool liveAuditAttempted)
    {
        sb.AppendLine("### Audit evidence — live sample (optional)");
        sb.AppendLine();

        if (!liveAuditAttempted)
        {
            sb.AppendLine(
                "_Not requested._ Pass **`--with-live-audit`** when the ArchLucid API is reachable with ReadAuthority credentials " +
                "(`ARCHLUCID_API_KEY` or bearer token) to attach a single-page `GET /v1/audit` summary for the active scope.");

            sb.AppendLine();

            return;
        }

        if (liveAudit is null)
        {
            sb.AppendLine("_Live audit fetch did not run._");

            sb.AppendLine();

            return;
        }

        if (!liveAudit.ApiReached)
        {
            sb.AppendLine($"_Live audit unavailable:_ {EscapeBackticks(liveAudit.ErrorNote ?? "unknown error")}");

            sb.AppendLine();

            return;
        }

        if (liveAudit.EventsInPage == 0)
        {
            sb.AppendLine("_API reachable; no audit rows returned for the current scope/page._");

            sb.AppendLine();

            return;
        }

        sb.AppendLine(
            $"Sample window: **{liveAudit.EventsInPage}** events in one page; occurred UTC range **{liveAudit.OldestUtc:O}** → **{liveAudit.NewestUtc:O}**.");

        sb.AppendLine();
        sb.AppendLine("| EventType | Count |");
        sb.AppendLine("| --- | --- |");

        foreach (KeyValuePair<string, int> pair in liveAudit.EventTypeCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {EscapePipe(pair.Key)} | {pair.Value.ToString(CultureInfo.InvariantCulture)} |");
        }

        sb.AppendLine();
    }

    private static string EscapeBackticks(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("`", "'", StringComparison.Ordinal);
    }

    private static string EscapePipe(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
