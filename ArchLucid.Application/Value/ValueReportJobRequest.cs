namespace ArchLucid.Application.Value;

public sealed record ValueReportJobRequest(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ProjectId,
    DateTimeOffset FromUtcInclusive,
    DateTimeOffset ToUtcExclusive);
