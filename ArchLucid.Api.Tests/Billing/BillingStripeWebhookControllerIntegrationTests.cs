using System.Net;
using System.Text;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>
///     HTTP coverage for <see cref="ArchLucid.Api.Controllers.Billing.BillingStripeWebhookController" /> without live Stripe (missing signature → provider rejects → 400).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class BillingStripeWebhookControllerIntegrationTests
{
    [SkippableFact]
    public async Task Stripe_post_without_signature_returns_bad_request()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        using StringContent content = new("{}", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/v1/billing/webhooks/stripe", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
