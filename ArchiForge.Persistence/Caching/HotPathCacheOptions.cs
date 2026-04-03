namespace ArchiForge.Persistence.Caching;

/// <summary>Controls optional in-process or Redis-backed caching for high-churn read paths (manifests, runs, policy pack metadata).</summary>
public sealed class HotPathCacheOptions
{
    public const string SectionName = "HotPathCache";

    /// <summary>When false, repositories hit SQL directly (no <see cref="IHotPathReadCache"/> registration in the API host).</summary>
    public bool Enabled { get; set; }

    /// <summary><c>Memory</c> (default) or <c>Redis</c> (requires <see cref="RedisConnectionString"/>).</summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>Absolute TTL for cached entries; clamped between 1 and 3600 seconds at runtime.</summary>
    public int AbsoluteExpirationSeconds { get; set; } = 60;

    /// <summary>StackExchange.Redis connection string when <see cref="Provider"/> is <c>Redis</c> (e.g. Azure Cache for Redis).</summary>
    public string RedisConnectionString { get; set; } = "";
}
