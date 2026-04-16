namespace ArchLucid.Core.Tenancy;

/// <summary>Row from <c>dbo.Tenants</c>.</summary>
public sealed class TenantRecord
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public TenantTier Tier { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset? SuspendedUtc { get; init; }
}
