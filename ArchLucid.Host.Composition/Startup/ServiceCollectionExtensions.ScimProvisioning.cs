using ArchLucid.Application.Scim;
using ArchLucid.Application.Scim.RoleMapping;
using ArchLucid.Application.Scim.Tokens;
using ArchLucid.Core.Configuration;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterScimProvisioning(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScimOptions>(configuration.GetSection(ScimOptions.SectionName));
        services.AddSingleton<IGroupToRoleMapper, GroupToRoleMapper>();
        services.AddScoped<IScimTokenIssuer, ScimTokenIssuer>();
        services.AddScoped<IScimBearerTokenAuthenticator, ScimBearerTokenAuthenticator>();
        services.AddScoped<IScimUserService, ScimUserService>();
        services.AddScoped<IScimGroupService, ScimGroupService>();
        services.AddHostedService<ScimTokenRotationReminderJob>();
    }
}
