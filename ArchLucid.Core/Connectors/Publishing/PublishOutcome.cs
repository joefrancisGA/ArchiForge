namespace ArchLucid.Core.Connectors.Publishing;

public sealed record PublishOutcome(
    bool Succeeded,
    string? ExternalPageId,
    ConfluencePublishFailureReason? FailureReason,
    string? ErrorMessage);
