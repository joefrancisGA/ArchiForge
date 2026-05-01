using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.Billing.Stripe;

using Microsoft.Extensions.Options;

using Moq;

using Stripe;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class StripeBillingProviderCheckoutWebhookIdempotencyTests
{
    /// <summary>
    ///     Must match the API version baked into the pinned <c>Stripe.net</c> package (see
    ///     <c>Directory.Packages.props</c>).
    /// </summary>
    private const string StripeNetWebhookApiVersion = "2025-08-27.basil";

    [SkippableFact]
    public async Task HandleWebhookAsync_duplicate_processed_event_returns_ok_without_replaying_mutation()
    {
        byte[] keyMaterial = new byte[32];
        Array.Fill(keyMaterial, (byte)7);
        string signingSecret = "whsec_" + Convert.ToBase64String(keyMaterial);

        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.Stripe,
            Stripe = new StripeBillingOptions { WebhookSigningSecret = signingSecret }
        };

        TestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        ledger
            .SetupSequence(l => l.TryInsertWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ledger
            .Setup(l => l.GetWebhookEventResultStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Processed");

        Mock<ITenantRepository> tenants = new();
        Mock<IAuditService> audit = new();
        BillingWebhookTrialActivator activator = new(ledger.Object, tenants.Object, audit.Object);
        Mock<IMarketplaceChangePlanWebhookMutationHandler> changePlan = new();
        StripeBillingProvider sut = new(monitor, ledger.Object, activator, changePlan.Object);

        Event stripeEvent = new()
        {
            Id = "evt_dup_test_ping_1", Type = "ping", ApiVersion = StripeNetWebhookApiVersion
        };

        string json = stripeEvent.ToJson();
        string signature = BuildStripeV1Signature(signingSecret, json);

        BillingWebhookHandleResult result = await sut.HandleWebhookAsync(
            new BillingWebhookInbound { RawBody = json, StripeSignatureHeader = signature },
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.DuplicateIgnored.Should().BeTrue();
        changePlan.Verify(
            h => h.HandleAsync(It.IsAny<Guid>(), It.IsAny<JsonElement>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static string BuildStripeV1Signature(string whsecSecret, string payload)
    {
        if (!whsecSecret.StartsWith("whsec_", StringComparison.Ordinal))
            throw new ArgumentException("Expected whsec_ prefix.", nameof(whsecSecret));

        // Stripe.net EventUtility.ComputeSignature uses UTF-8 bytes of the full secret string (v48.x), not whsec_ base64 decode.
        byte[] key = Encoding.UTF8.GetBytes(whsecSecret);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signedPayload = $"{timestamp}.{payload}";

        using HMACSHA256 hmac = new(key);
        byte[] mac = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        string hex = Convert.ToHexString(mac).ToLowerInvariant();

        return $"t={timestamp},v1={hex}";
    }

    private sealed class TestMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue
        {
            get;
        } = value;

        public T Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return null;
        }
    }
}
