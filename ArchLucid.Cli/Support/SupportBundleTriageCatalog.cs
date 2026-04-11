namespace ArchLucid.Cli.Support;

/// <summary>Canonical triage order kept in sync with <see cref="SupportBundleReadme"/> and <see cref="SupportBundleArchiveWriter"/> file names.</summary>
public static class SupportBundleTriageCatalog
{
    public static IReadOnlyList<SupportBundleTriageEntry> Entries
    {
        get;
    } =
    [
        new()
        {
            File = SupportBundleArchiveWriter.HealthFileName,
            Why = "Liveness/readiness/combined health — start here when the API misbehaves.",
        },
        new()
        {
            File = SupportBundleArchiveWriter.BuildFileName,
            Why = "CLI identity plus GET /version — server build and environment name.",
        },
        new()
        {
            File = SupportBundleArchiveWriter.ApiContractFileName,
            Why = "GET /openapi/v1.json probe — confirms Microsoft OpenAPI document is served (truncated body).",
        },
        new()
        {
            File = SupportBundleArchiveWriter.ConfigFileName,
            Why = "Non-secret archlucid.json summary and resolved API URL (redacted).",
        },
        new()
        {
            File = SupportBundleArchiveWriter.EnvironmentFileName,
            Why = "Safe environment snapshot for local CLI/runtime context.",
        },
        new()
        {
            File = SupportBundleArchiveWriter.WorkspaceFileName,
            Why = "Outputs/cache folder presence and size.",
        },
        new()
        {
            File = SupportBundleArchiveWriter.LogsFileName,
            Why = "Optional local last-run log excerpt.",
        },
        new()
        {
            File = SupportBundleArchiveWriter.ReferencesFileName,
            Why = "Documentation links and correlation ID triage hints.",
        },
    ];
}
