namespace ArchLucid.Core.Pagination;

/// <summary>Keyset paging for synthesized artifact descriptors (manifest bundle).</summary>
public static class ArtifactListingPagination
{
    public const int DefaultTake = 50;

    public const int MaxTake = 200;

    public static int ClampTake(int take) => Math.Clamp(take <= 0 ? DefaultTake : take, 1, MaxTake);
}
