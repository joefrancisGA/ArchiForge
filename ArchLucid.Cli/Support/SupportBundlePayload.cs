namespace ArchLucid.Cli.Support;

public sealed record SupportBundlePayload(
    SupportBundleManifest Manifest,
    SupportBundleBuildSection Build,
    SupportBundleHealthSection Health,
    SupportBundleApiContractSection ApiContract,
    SupportBundleConfigSummary ConfigSummary,
    SupportBundleEnvironmentSection Environment,
    SupportBundleWorkspaceSection Workspace,
    SupportBundleReferencesSection References,
    SupportBundleLogsSection Logs);
