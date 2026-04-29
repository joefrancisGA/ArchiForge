namespace ArchLucid.Core.Pagination;

/// <summary>
///     Run list pagination defaults aligned with perf plan (narrower than generic <see cref="PaginationDefaults" /> caps).
/// </summary>
public static class RunPagination
{
    /// <summary>Run list fetch when unset or invalid.</summary>
    public const int DefaultTake = 25;

    /// <summary>
    ///     Run list ceiling for keyset paths (FINDINGS_ARTIFACT_AUDIT_* use wider limits separately).
    /// </summary>
    public const int MaxTake = 100;

    /// <summary>Clamp <paramref name="take" /> to <see cref="DefaultTake" />..<see cref="MaxTake" />.</summary>
    public static int ClampTake(int take) => Math.Clamp(take <= 0 ? DefaultTake : take, 1, MaxTake);
}
