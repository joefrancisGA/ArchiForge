using System.Net;
using System.Net.Http.Json;

using ArchLucid.Application.Notifications.Email;
using ArchLucid.Contracts.Marketing;
using ArchLucid.Persistence.Marketing;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>POST /v1/marketing/pricing/quote-request</c> — persist + sales notification path.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class MarketingPricingQuoteRequestEndpointTests
{
    private static readonly Guid SeededRequestId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task PostQuoteRequest_after_successful_persist_invokes_sales_notifier()
    {
        DateTime createdUtc = new(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);
        SeededQuoteRepository repo = new(
            new MarketingPricingQuoteRequestInsertResult(SeededRequestId, createdUtc));

        Mock<IMarketingPricingQuoteSalesNotifier> notifier = new();
        notifier
            .Setup(
                n => n.NotifyAsync(
                    It.IsAny<MarketingPricingQuoteRequestInsertResult>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using OpenApiContractWebAppFactory baseFactory = new();
        await using WebApplicationFactory<Program> factory = baseFactory.WithWebHostBuilder(
            b => b.ConfigureTestServices(
                services =>
                {
                    services.RemoveAll<IMarketingPricingQuoteRequestRepository>();
                    services.AddSingleton<IMarketingPricingQuoteRequestRepository>(repo);
                    services.RemoveAll<IMarketingPricingQuoteSalesNotifier>();
                    services.AddSingleton(notifier.Object);
                }));

        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1/marketing/pricing/quote-request",
            new
            {
                workEmail = "buyer@example.com",
                companyName = "Contoso",
                tierInterest = "Team",
                message = "Please send a quote",
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        notifier.Verify(
            n => n.NotifyAsync(
                It.Is<MarketingPricingQuoteRequestInsertResult>(
                    r => r.Id == SeededRequestId && r.CreatedUtc == createdUtc),
                "buyer@example.com",
                "Contoso",
                "Team",
                "Please send a quote",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class SeededQuoteRepository(MarketingPricingQuoteRequestInsertResult insert)
        : IMarketingPricingQuoteRequestRepository
    {
        private readonly MarketingPricingQuoteRequestInsertResult _insert = insert;

        public Task<MarketingPricingQuoteRequestInsertResult?> AppendAsync(
            string workEmail,
            string companyName,
            string tierInterest,
            string message,
            byte[]? clientIpSha256,
            CancellationToken cancellationToken) =>
            Task.FromResult<MarketingPricingQuoteRequestInsertResult?>(_insert);
    }
}
