namespace ArchLucid.Persistence.Coordination.Caching;

/// <summary>Resolves <see cref="HotPathCacheOptions.Provider"/> <c>Auto</c> to an effective backing store.</summary>
public static class HotPathCacheProviderResolver
{
    /// <summary>
    /// Returns <c>Redis</c> or <c>Memory</c> for registration. <c>Auto</c> maps to Redis only when replica count &gt; 1 and a Redis connection string exists.
    /// </summary>
    public static string ResolveEffectiveProvider(HotPathCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string raw = options.Provider ?? "Memory";

        if (string.Equals(raw, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            if (options.ExpectedApiReplicaCount > 1 &&
                !string.IsNullOrWhiteSpace(options.RedisConnectionString))
                return "Redis";

            return "Memory";
        }

        return raw.Trim();
    }
}
