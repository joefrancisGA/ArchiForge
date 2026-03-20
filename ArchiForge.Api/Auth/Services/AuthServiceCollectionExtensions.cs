using System.Security.Claims;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ArchiForgeAuthOptions>(
            configuration.GetSection(ArchiForgeAuthOptions.SectionName));

        var authOptions = configuration
                .GetSection(ArchiForgeAuthOptions.SectionName)
                .Get<ArchiForgeAuthOptions>()
            ?? new ArchiForgeAuthOptions();

        if (string.Equals(authOptions.Mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = authOptions.Authority;
                    options.Audience = authOptions.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = !string.IsNullOrWhiteSpace(authOptions.Audience),
                        RoleClaimType = "roles",
                        NameClaimType = ClaimTypes.Name
                    };
                });
        }
        else
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DevelopmentBypassAuthenticationHandler>(
                    DevelopmentBypassAuthenticationHandler.SchemeName,
                    _ => { });
        }

        services.AddScoped<IClaimsTransformation, ArchiForgeRoleClaimsTransformation>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ArchiForgePolicies.ReadAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchiForgeRoles.Reader,
                    ArchiForgeRoles.Operator,
                    ArchiForgeRoles.Admin);
            });

            options.AddPolicy(ArchiForgePolicies.ExecuteAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ArchiForgeRoles.Operator,
                    ArchiForgeRoles.Admin);
            });

            options.AddPolicy(ArchiForgePolicies.AdminAuthority, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(ArchiForgeRoles.Admin);
            });
        });

        return services;
    }
}
