namespace ArchLucid.Core.Scim.Models;

/// <summary>Active SCIM bearer token ageing input for rotation reminder job (token id only — no secrets).</summary>
public sealed record ScimTokenRotationCandidate(Guid Id, Guid TenantId, DateTimeOffset CreatedUtc);
