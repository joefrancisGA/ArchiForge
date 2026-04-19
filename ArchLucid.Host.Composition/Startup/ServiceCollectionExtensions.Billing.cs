using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.Billing.AzureMarketplace;
using ArchLucid.Persistence.Billing.Stripe;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    internal static void RegisterBilling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BillingOptions>(configuration.GetSection(BillingOptions.SectionName));
        services.AddHttpClient(nameof(AzureMarketplaceBillingProvider));
        services.AddScoped<BillingWebhookTrialActivator>();
        services.AddScoped<StripeBillingProvider>();
        services.AddScoped<NoopBillingProvider>();
        services.AddScoped<AzureMarketplaceBillingProvider>();
        services.AddScoped<IMarketplaceWebhookTokenVerifier, MicrosoftMarketplaceJwtVerifier>();
        services.AddScoped<IBillingProviderRegistry, BillingProviderRegistry>();
        services.AddScoped<IBillingTrialConversionGate, BillingTrialConversionGate>();
    }
}
