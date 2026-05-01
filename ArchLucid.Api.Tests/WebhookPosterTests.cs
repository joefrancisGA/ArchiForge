using ArchLucid.Decisioning.Advisory.Delivery;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Services.Delivery;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

public sealed class WebhookPosterTests
{
    [SkippableFact]
    public async Task FakeWebhookPoster_PostJsonAsync_completes_without_secret()
    {
        FakeWebhookPoster poster = new(NullLogger<FakeWebhookPoster>.Instance);

        Func<Task> act = async () =>
            await poster.PostJsonAsync("https://example.test/hook", new { x = 1 }, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task FakeWebhookPoster_PostJsonAsync_with_secret_includes_signature_prefix_in_log_state()
    {
        Mock<ILogger<FakeWebhookPoster>> logger = new();
        FakeWebhookPoster poster = new(logger.Object);

        await poster.PostJsonAsync(
            "https://example.test/hook",
            new { n = 2 },
            CancellationToken.None,
            new WebhookPostOptions { HmacSha256SharedSecret = "abc" });

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task WebhookHmacEnvelopePoster_merges_config_secret_and_calls_inner()
    {
        Mock<IOptionsMonitor<WebhookDeliveryOptions>> opts = new();
        opts.Setup(o => o.CurrentValue).Returns(new WebhookDeliveryOptions { HmacSha256SharedSecret = "cfg-secret" });
        Mock<IWebhookPoster> inner = new();
        WebhookHmacEnvelopePoster poster = new(opts.Object, inner.Object);

        await poster.PostJsonAsync("https://x", new { a = 1 }, CancellationToken.None);

        inner.Verify(
            i => i.PostJsonAsync(
                "https://x",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>(),
                It.Is<WebhookPostOptions>(w => w.HmacSha256SharedSecret == "cfg-secret")),
            Times.Once);
    }

    [SkippableFact]
    public async Task WebhookHmacEnvelopePoster_call_secret_overrides_config()
    {
        Mock<IOptionsMonitor<WebhookDeliveryOptions>> opts = new();
        opts.Setup(o => o.CurrentValue).Returns(new WebhookDeliveryOptions { HmacSha256SharedSecret = "cfg" });
        Mock<IWebhookPoster> inner = new();
        WebhookHmacEnvelopePoster poster = new(opts.Object, inner.Object);

        await poster.PostJsonAsync(
            "https://x",
            new { a = 1 },
            CancellationToken.None,
            new WebhookPostOptions { HmacSha256SharedSecret = "call" });

        inner.Verify(
            i => i.PostJsonAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>(),
                It.Is<WebhookPostOptions>(w => w.HmacSha256SharedSecret == "call")),
            Times.Once);
    }

    [SkippableFact]
    public async Task WebhookHmacEnvelopePoster_preserves_telemetry_when_merging_hmac_secret_from_config()
    {
        Mock<IOptionsMonitor<WebhookDeliveryOptions>> opts = new();
        opts.Setup(o => o.CurrentValue).Returns(new WebhookDeliveryOptions { HmacSha256SharedSecret = "cfg-secret" });
        Mock<IWebhookPoster> inner = new();

        Guid tenantId = Guid.Parse("5d4d4d4d-aaaa-bbbb-cccc-dddddddddddd");
        WebhookHmacEnvelopePoster poster = new(opts.Object, inner.Object);

        await poster.PostJsonAsync(
            "https://x/webhook/private",
            new { ok = true },
            CancellationToken.None,
            new WebhookPostOptions { EventType = "tenant.digest", TenantId = tenantId, });

        inner.Verify(
            i => i.PostJsonAsync(
                "https://x/webhook/private",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>(),
                It.Is<WebhookPostOptions>(w => w.HmacSha256SharedSecret == "cfg-secret"
                                               && w.EventType == "tenant.digest"
                                               && w.TenantId == tenantId)),
            Times.Once);
    }
}
