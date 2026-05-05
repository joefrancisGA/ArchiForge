using ArchLucid.Application.Identity;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Identity;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class PwnedPasswordRangeClientTests
{
    [SkippableFact]
    public async Task IsPasswordPwnedAsync_when_disabled_skips_http()
    {
        CountingHandler handler = new();
        using HttpClient http = new(handler);
        using MemoryCache cache = new(Options.Create(new MemoryCacheOptions()));
        PwnedPasswordRangeClient sut = CreateClient(http, cache, pwnedEnabled: false);

        bool pwned = await sut.IsPasswordPwnedAsync("any-password", CancellationToken.None);

        pwned.Should().BeFalse();
        handler.SendCount.Should().Be(0);
    }

    [SkippableFact]
    public void RangeResponseCacheDuration_is_24_hours()
    {
        PwnedPasswordRangeClient.RangeResponseCacheDuration.Should().Be(TimeSpan.FromHours(24));
    }

    [SkippableFact]
    public async Task IsPasswordPwnedAsync_reuses_range_cache_per_prefix()
    {
        CountingHandler handler = new();
        using HttpClient http = new(handler);
        using MemoryCache cache = new(Options.Create(new MemoryCacheOptions()));
        PwnedPasswordRangeClient sut = CreateClient(http, cache, pwnedEnabled: true);
        const string password = "stable-password-for-prefix-cache";

        bool first = await sut.IsPasswordPwnedAsync(password, CancellationToken.None);
        bool second = await sut.IsPasswordPwnedAsync(password, CancellationToken.None);

        first.Should().BeFalse();
        second.Should().BeFalse();
        handler.SendCount.Should().Be(1);
    }

    private static PwnedPasswordRangeClient CreateClient(HttpClient http, IMemoryCache cache, bool pwnedEnabled)
    {
        TrialAuthOptions options = new() { LocalIdentity = new TrialLocalIdentityOptions { PwnedPasswordRangeCheckEnabled = pwnedEnabled }, };

        Mock<IOptions<TrialAuthOptions>> mo = new();
        mo.Setup(x => x.Value).Returns(options);

        return new PwnedPasswordRangeClient(http, cache, mo.Object);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int SendCount
        {
            get;
            private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;

            return Task.FromResult(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(string.Empty), });
        }
    }
}
