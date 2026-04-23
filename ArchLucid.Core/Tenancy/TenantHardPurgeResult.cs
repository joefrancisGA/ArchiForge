namespace ArchLucid.Core.Tenancy;

/// <summary>Outcome of <see cref="ITenantHardPurgeService.PurgeTenantAsync" />.</summary>
public sealed class TenantHardPurgeResult
{
    public int RowsDeleted
    {
        get;
        init;
    }

    public IReadOnlyDictionary<string, int> RowCountsByTable
    {
        get;
        init;
    } =
        new Dictionary<string, int>(StringComparer.Ordinal);
}
