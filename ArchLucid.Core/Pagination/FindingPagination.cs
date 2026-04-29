namespace ArchLucid.Core.Pagination;

/// <summary>Defaults for keyset-paged finding record lists (narrower than generic listing caps).</summary>
public static class FindingPagination
{
    public const int DefaultTake = 50;

    public const int MaxTake = 200;

    public static int ClampTake(int take) => Math.Clamp(take <= 0 ? DefaultTake : take, 1, MaxTake);
}
