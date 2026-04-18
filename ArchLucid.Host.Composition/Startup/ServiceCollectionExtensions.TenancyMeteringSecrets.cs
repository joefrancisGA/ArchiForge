using ArchLucid.Application.Identity;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Secrets;
using ArchLucid.Core.Tenancy;
using ArchLucid.Host.Core.Configuration.Secrets;
using ArchLucid.Persistence.Metering;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterTenancyMeteringAndSecrets(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ArchLucid.Core.Metering.MeteringOptions>(
            configuration.GetSection(ArchLucid.Core.Metering.MeteringOptions.SectionName));
        services.Configure<ArchLucidSecretOptions>(configuration.GetSection(ArchLucidSecretOptions.SectionName));
        services.Configure<TrialAuthOptions>(configuration.GetSection(TrialAuthOptions.SectionPath));
        services.Configure<TrialLifecycleSchedulerOptions>(
            configuration.GetSection(TrialLifecycleSchedulerOptions.SectionName));

        services.AddScoped<ITrialBootstrapEmailVerificationPolicy, TrialBootstrapEmailVerificationPolicy>();
        services.AddSingleton<PasswordHasher<TrialIdentityHasherUser>>();
        services.AddSingleton<TrialPasswordPolicyValidator>();
        services.AddHttpClient<PwnedPasswordRangeClient>(
            client =>
            {
                client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Add-Padding", "true");
            });
        services.AddScoped<ITrialLocalIdentityService, TrialLocalIdentityService>();

        services.AddScoped<IUsageMeteringService, UsageMeteringService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<ITrialTenantBootstrapService, TrialTenantBootstrapService>();
        services.AddScoped<TrialLimitGate>();
        services.AddScoped<TrialSeatAccountant>();
        services.AddScoped<TrialLifecycleTransitionEngine>();

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
