namespace ArchLucid.Cli.Commands;

/// <summary>Summarizes `validate-config` findings without emitting secrets.</summary>
internal static class ComplianceReportValidateConfigSummaryFormatter
{
    internal static string Build(IReadOnlyList<ValidateConfigFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        int errors = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Error);
        int warnings = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Warning);
        int oks = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Ok);
        int infos = findings.Count(f => f.Severity == ValidateConfigFindingSeverity.Info);

        List<string> lines =
        [
            "| Severity | Count |",
            "|----------|-------|",
            $"| Error | {errors} |",
            $"| Warning | {warnings} |",
            $"| Ok | {oks} |",
            $"| Info | {infos} |",
            "",
            "**Error and warning checks (operator follow-up):**",
            "",
        ];

        foreach (ValidateConfigFinding f in findings
                     .Where(x => x.Severity is ValidateConfigFindingSeverity.Error or ValidateConfigFindingSeverity.Warning)
                     .OrderBy(x => x.Severity)
                     .ThenBy(x => x.Category, StringComparer.Ordinal)
                     .ThenBy(x => x.Check, StringComparer.Ordinal))
        {
            lines.Add($"- **{f.Severity}** · `{f.Category}` · `{f.Check}` — {f.Detail}");
        }

        if (errors == 0 && warnings == 0)
            lines.Add("- *(no errors or warnings)*");

        return string.Join(Environment.NewLine, lines);
    }
}
