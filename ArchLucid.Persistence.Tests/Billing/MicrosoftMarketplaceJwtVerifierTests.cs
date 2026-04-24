using System.Security.Claims;

using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Billing.AzureMarketplace;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class MicrosoftMarketplaceJwtVerifierTests
{
    [Fact]
    public async Task ValidateAsync_without_metadata_returns_null()
    {
        BillingOptions billing = new()
        {
            AzureMarketplace = new AzureMarketplaceBillingOptions
            {
                OpenIdMetadataAddress = null, ValidAudiences = ["https://marketplaceapi.microsoft.com"]
            }
        };

        TestMonitor<BillingOptions> monitor = new(billing);
        MicrosoftMarketplaceJwtVerifier sut = new(monitor);

        ClaimsPrincipal? principal =
            await sut.ValidateAsync("any.jwt.here", CancellationToken.None);

        principal.Should().BeNull();
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
