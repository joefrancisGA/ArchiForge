using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Secrets;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Configuration.Secrets;
using ArchLucid.Persistence.Metering;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterTenancyMeteringAndSecrets(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MeteringOptions>(configuration.GetSection(MeteringOptions.SectionName));
        services.Configure<ArchLucidSecretOptions>(configuration.GetSection(ArchLucidSecretOptions.SectionName));

        services.AddScoped<IUsageMeteringService, UsageMeteringService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        services.AddSingleton<ISecretProvider>(sp =>
        {
            IOptions<ArchLucidSecretOptions> options = sp.GetRequiredService<IOptions<ArchLucidSecretOptions>>();
            ArchLucidSecretOptions o = options.Value;

            if (o.Provider == SecretProviderKind.KeyVault)
            {
                return new KeyVaultSecretProvider(
                    Options.Create(o),
                    sp.GetRequiredService<IMemoryCache>());
            }

            return new EnvironmentVariableSecretProvider(sp.GetRequiredService<IConfiguration>());
        });
    }
}
