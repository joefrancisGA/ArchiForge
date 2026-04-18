namespace ArchLucid.Core.Tenancy;

/// <summary>Options for <see cref="ITenantHardPurgeService"/>.</summary>
public sealed class TenantHardPurgeOptions
{
    public bool DryRun { get; init; }

    public int MaxRowsPerStatement { get; init; } = 5000;
}
