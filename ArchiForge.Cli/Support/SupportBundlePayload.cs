namespace ArchiForge.Cli.Support;

public sealed record SupportBundlePayload(
    SupportBundleManifest Manifest,
    SupportBundleBuildSection Build,
    SupportBundleHealthSection Health,
    SupportBundleConfigSummary ConfigSummary,
    SupportBundleEnvironmentSection Environment,
    SupportBundleWorkspaceSection Workspace,
    SupportBundleReferencesSection References,
    SupportBundleLogsSection Logs);
