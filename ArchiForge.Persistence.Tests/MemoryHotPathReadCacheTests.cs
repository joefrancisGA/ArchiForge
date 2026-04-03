using ArchiForge.Persistence.Caching;
using ArchiForge.Persistence.Options;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchiForge.Persistence.Tests;

public sealed class MemoryHotPathReadCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_second_call_does_not_invoke_factory()
    {
        int calls = 0;
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache cache = new(new MemoryCache(new MemoryCacheOptions()), monitor);

        async Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return await Task.FromResult("x");
        }

        string? first = await cache.GetOrCreateAsync("k1", Factory, CancellationToken.None);
        string? second = await cache.GetOrCreateAsync("k1", Factory, CancellationToken.None);

        first.Should().Be("x");
        second.Should().Be("x");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_does_not_cache_null()
    {
        int calls = 0;
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache cache = new(new MemoryCache(new MemoryCacheOptions()), monitor);

        async Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return await Task.FromResult<string?>(null);
        }

        string? a = await cache.GetOrCreateAsync("k-null", Factory, CancellationToken.None);
        string? b = await cache.GetOrCreateAsync("k-null", Factory, CancellationToken.None);

        a.Should().BeNull();
        b.Should().BeNull();
        calls.Should().Be(2);
    }

    [Fact]
    public async Task RemoveAsync_drops_entry()
    {
        HotPathCacheOptions options = new() { AbsoluteExpirationSeconds = 60 };
        IOptionsMonitor<HotPathCacheOptions> monitor = new FixedOptionsMonitor<HotPathCacheOptions>(options);
        MemoryHotPathReadCache cache = new(new MemoryCache(new MemoryCacheOptions()), monitor);

        int calls = 0;

        async Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return await Task.FromResult("v");
        }

        await cache.GetOrCreateAsync("evict", Factory, CancellationToken.None);
        await cache.RemoveAsync("evict", CancellationToken.None);
        await cache.GetOrCreateAsync("evict", Factory, CancellationToken.None);

        calls.Should().Be(2);
    }
}
