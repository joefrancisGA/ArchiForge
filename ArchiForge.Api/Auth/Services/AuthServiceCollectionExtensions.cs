using System.Security.Claims;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Authentication;
using ArchiForge.Api.Configuration;
using ArchiForge.Host.Core.Configuration;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ArchiForge.Api.Auth.Services;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Well-known scheme name used when <c>ArchiForgeAuth:Mode</c> is <c>ApiKey</c>.</summary>
    public const string ApiKeySchemeName = "ApiKey";

    public static IServiceCollection AddArchiForgeAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ArchiForgeAuthOptions>(configuration.GetSection(ArchiForgeAuthOptions.SectionName));
        services.PostConfigure<ArchiForgeAuthOptions>(opts =>
        {
            IConfigurationSection lucid = configuration.GetSection(ArchiForgeConfigurationBridge.ArchLucidAuthSectionName);

            if (lucid.Exists())
            {
                lucid.Bind(opts);
            }
        });

        ArchiForgeAuthOptions authOptions = ArchiForgeAuthConfigurationBridge.Resolve(configuration);

        if (string.Equals(authOptions.Mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))

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
                    string nameClaimType = string.IsNullOrWhiteSpace(authOptions.NameClaimType)
                        ? ClaimTypes.Name
                        : authOptions.NameClaimType.Trim();
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = !string.IsNullOrWhiteSpace(authOptions.Audience),
                        RoleClaimType = "roles",
                        NameClaimType = nameClaimType
                    };
                });

        else if (string.Equals(authOptions.Mode, "ApiKey", StringComparison.OrdinalIgnoreCase))

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeySchemeName;
                    options.DefaultChallengeScheme = ApiKeySchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeySchemeName,
                    _ => { });

        else

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DevelopmentBypassAuthenticationHandler>(
                    DevelopmentBypassAuthenticationHandler.SchemeName,
                    _ => { });


        services.AddScoped<IClaimsTransformation, ArchiForgeRoleClaimsTransformation>();

        return services;
    }
}
