namespace ArchLucid.Core.Connectors.Publishing;

/// <summary>Immutable snapshot handed to a publisher after the fan-out handler computed diff + badge.</summary>
public sealed record PublishRequest(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid TargetId,
    Guid RunId,
    string ManifestVersion,
    string DiffBadgeStateLabel,
    string PayloadJson,
    string PageTitle,
    string? ExistingConfluencePageId);
