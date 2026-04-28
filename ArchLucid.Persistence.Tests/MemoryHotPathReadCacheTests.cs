using ArchLucid.Persistence.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Tests;

public sealed class MemoryHotPathReadCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_second_call_does_not_invoke_factory()
    {
        int calls = 0;
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache cache = new(new MemoryCache(new MemoryCacheOptions()), monitor);

        string? first = await cache.GetOrCreateAsync("k1", Factory, CancellationToken.None);
        string? second = await cache.GetOrCreateAsync("k1", Factory, CancellationToken.None);

        first.Should().Be("x");
        second.Should().Be("x");
        calls.Should().Be(1);
        return;

        async Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return await Task.FromResult("x");
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_does_not_cache_null()
    {
        int calls = 0;
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache cache = new(new MemoryCache(new MemoryCacheOptions()), monitor);

        string? a = await cache.GetOrCreateAsync("k-null", Factory, CancellationToken.None);
        string? b = await cache.GetOrCreateAsync("k-null", Factory, CancellationToken.None);

        a.Should().BeNull();
        b.Should().BeNull();
        calls.Should().Be(2);
        return;

        async Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return await Task.FromResult<string?>(null);
        }
    }

    [Fact]
    public async Task RemoveAsync_drops_entry()
    {
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache cache = new(new MemoryCache(new MemoryCacheOptions()), monitor);

        int calls = 0;

        await cache.GetOrCreateAsync("evict", Factory, CancellationToken.None);
        await cache.RemoveAsync("evict", CancellationToken.None);
        await cache.GetOrCreateAsync("evict", Factory, CancellationToken.None);

        calls.Should().Be(2);
        return;

        async Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return await Task.FromResult("v");
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_promotes_legacy_key_to_primary()
    {
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryCache backing = new(new MemoryCacheOptions());

        using (ICacheEntry entry = backing.CreateEntry("old-key"))
        {
            entry.Value = "legacy-value";
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        }

        MemoryHotPathReadCache cache = new(backing, monitor);

        string? result = await cache.GetOrCreateAsync(
            "new-key",
            _ => Task.FromResult<string?>("from-factory"),
            CancellationToken.None,
            "old-key");

        result.Should().Be("legacy-value");
        backing.TryGetValue("new-key", out object? promoted).Should().BeTrue();
        promoted.Should().Be("legacy-value");
        backing.TryGetValue("old-key", out _).Should().BeFalse();

        int factoryCalls = 0;

        string? second = await cache.GetOrCreateAsync(
            "new-key",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<string?>("should-not-run");
            },
            CancellationToken.None);

        second.Should().Be("legacy-value");
        factoryCalls.Should().Be(0);
    }
}
