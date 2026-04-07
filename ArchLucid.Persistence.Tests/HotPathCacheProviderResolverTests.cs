using ArchLucid.Persistence.Coordination.Caching;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests;

public sealed class HotPathCacheProviderResolverTests
{
    [Theory]
    [InlineData("Memory", 1, "", "Memory")]
    [InlineData("Redis", 1, "localhost:6379", "Redis")]
    [InlineData("Auto", 1, "", "Memory")]
    [InlineData("Auto", 1, "localhost:6379", "Memory")]
    [InlineData("Auto", 2, "", "Memory")]
    [InlineData("Auto", 2, "localhost:6379", "Redis")]
    public void ResolveEffectiveProvider_returns_expected(string provider, int replicas, string redis, string expected)
    {
        HotPathCacheOptions options = new()
        {
            Provider = provider,
            ExpectedApiReplicaCount = replicas,
            RedisConnectionString = redis
        };

        HotPathCacheProviderResolver.ResolveEffectiveProvider(options).Should().Be(expected);
    }
}
