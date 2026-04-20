using ArchLucid.Core.Billing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>Extends <see cref="GreenfieldSqlApiFactory"/> with Azure Marketplace billing settings and a stub JWT verifier.</summary>
internal abstract class BillingMarketplaceWebhookApiFactoryBase : GreenfieldSqlApiFactory
{
    protected abstract bool GaEnabled { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration(
            (_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Billing:Provider"] = "AzureMarketplace",
                    ["Billing:AzureMarketplace:FulfillmentApiEnabled"] = "false",
                    ["Billing:AzureMarketplace:GaEnabled"] = GaEnabled ? "true" : "false",
                    ["Billing:AzureMarketplace:LandingPageUrl"] = "https://billing-test.invalid/landing",
                    ["Billing:AzureMarketplace:OpenIdMetadataAddress"] =
                        "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                    ["Billing:AzureMarketplace:ValidAudiences:0"] = "https://marketplaceapi.microsoft.com",
                }));

        builder.ConfigureTestServices(
            services =>
            {
                services.RemoveAll<IMarketplaceWebhookTokenVerifier>();
                services.AddSingleton<IMarketplaceWebhookTokenVerifier, AcceptAnyMarketplaceJwtVerifier>();
            });
    }
}

internal sealed class BillingMarketplaceWebhookDeferredApiFactory : BillingMarketplaceWebhookApiFactoryBase
{
    protected override bool GaEnabled => false;
}

internal sealed class BillingMarketplaceWebhookGaOnApiFactory : BillingMarketplaceWebhookApiFactoryBase
{
    protected override bool GaEnabled => true;
}
