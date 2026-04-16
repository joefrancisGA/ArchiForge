namespace ArchLucid.Core.Tenancy;

/// <summary>Commercial tier for a provisioned tenant (stored as string in SQL).</summary>
public enum TenantTier
{
    Free = 0,

    Standard = 1,

    Enterprise = 2,
}
