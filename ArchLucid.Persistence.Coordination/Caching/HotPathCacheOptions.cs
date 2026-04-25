using ArchLucid.Persistence.Caching;

namespace ArchLucid.Persistence.Coordination.Caching;

/// <summary>
///     Controls optional in-process or Redis-backed caching for high-churn read paths (manifests, runs, policy pack
///     metadata).
/// </summary>
public sealed class HotPathCacheOptions
{
    public const string SectionName = "HotPathCache";

    /// <summary>
    ///     When false, repository decorators hit SQL directly; the API host may still register a small in-memory
    ///     <see cref="IHotPathReadCache" /> for optional read-model caches that must not imply SQL hot-path caching is on.
    /// </summary>
    public bool Enabled
    {
        get;
        set;
    }

    /// <summary>
    ///     <c>Memory</c>, <c>Redis</c> (requires <see cref="RedisConnectionString" />), or <c>Auto</c>:
    ///     when <see cref="ExpectedApiReplicaCount" /> is greater than 1 and <see cref="RedisConnectionString" /> is set, uses
    ///     Redis; otherwise Memory.
    /// </summary>
    public string Provider
    {
        get;
        set;
    } = "Memory";

    /// <summary>
    ///     Declared maximum (or typical) API replica count for this deployment. When <see cref="Provider" /> is <c>Auto</c>
    ///     and this is &gt; 1,
    ///     Redis is selected if <see cref="RedisConnectionString" /> is configured. Align with Container Apps
    ///     <c>max_replicas</c> when scale-out is allowed.
    /// </summary>
    public int ExpectedApiReplicaCount
    {
        get;
        set;
    } = 1;

    /// <summary>Absolute TTL for cached entries; clamped between 1 and 3600 seconds at runtime.</summary>
    public int AbsoluteExpirationSeconds
    {
        get;
        set;
    } = 60;

    /// <summary>
    ///     StackExchange.Redis connection string when <see cref="Provider" /> is <c>Redis</c> (e.g. Azure Cache for
    ///     Redis).
    /// </summary>
    public string RedisConnectionString
    {
        get;
        set;
    } = "";
}
