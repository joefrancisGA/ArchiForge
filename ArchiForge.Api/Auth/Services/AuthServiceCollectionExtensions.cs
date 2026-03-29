using System.Security.Claims;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Authentication;

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
        services.Configure<ArchiForgeAuthOptions>(
            configuration.GetSection(ArchiForgeAuthOptions.SectionName));

        ArchiForgeAuthOptions authOptions = configuration
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
        else if (string.Equals(authOptions.Mode, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeySchemeName;
                    options.DefaultChallengeScheme = ApiKeySchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeySchemeName,
                    _ => { });
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

        return services;
    }
}
