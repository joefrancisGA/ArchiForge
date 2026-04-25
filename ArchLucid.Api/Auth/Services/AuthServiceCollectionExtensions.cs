using System.Security.Claims;
using System.Security.Cryptography;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Auth.Scim;
using ArchLucid.Api.Authentication;
using ArchLucid.Api.Configuration;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Auth.Services;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Well-known scheme name used when <c>ArchLucidAuth:Mode</c> is <c>ApiKey</c>.</summary>
    public const string ApiKeySchemeName = "ApiKey";

    public static IServiceCollection AddArchLucidAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ArchLucidAuthOptions>(configuration.GetSection(ArchLucidAuthOptions.SectionName));
        services.Configure<ApiKeyAuthenticationOptions>(
            configuration.GetSection(ApiKeyAuthenticationOptions.SectionPath));

        ArchLucidAuthOptions authOptions = ArchLucidAuthConfigurationBridge.Resolve(configuration);

        if (string.Equals(authOptions.Mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => ConfigureJwtBearer(options, authOptions, configuration))
                .AddScheme<AuthenticationSchemeOptions, ScimBearerAuthenticationHandler>(
                    ScimBearerDefaults.AuthenticationScheme,
                    _ =>
                    {
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
                    _ =>
                    {
                    })
                .AddScheme<AuthenticationSchemeOptions, ScimBearerAuthenticationHandler>(
                    ScimBearerDefaults.AuthenticationScheme,
                    _ =>
                    {
                    });

        else

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DevelopmentBypassAuthenticationHandler>(
                    DevelopmentBypassAuthenticationHandler.SchemeName,
                    _ =>
                    {
                    })
                .AddScheme<AuthenticationSchemeOptions, ScimBearerAuthenticationHandler>(
                    ScimBearerDefaults.AuthenticationScheme,
                    _ =>
                    {
                    });


        services.AddScoped<IClaimsTransformation, ArchLucidRoleClaimsTransformation>();

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, ArchLucidAuthOptions authOptions,
        IConfiguration configuration)
    {
        string pemPath = authOptions.JwtSigningPublicKeyPemPath.Trim();

        if (!string.IsNullOrEmpty(pemPath))
        {
            ConfigureJwtBearerWithLocalPublicKey(options, authOptions, pemPath);

            return;
        }

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

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, authOptions);

        TrialAuthOptions trial =
            configuration.GetSection(TrialAuthOptions.SectionPath).Get<TrialAuthOptions>() ?? new TrialAuthOptions();

        bool trialExternalId = TrialAuthModeConstants.HasMode(trial.Modes, TrialAuthModeConstants.MsaExternalId);

        TrialExternalIdJwtBearerSupport.TryAllowConsumerIdentityIssuers(options, trialExternalId);
    }

    private static void ConfigureJwtBearerWithLocalPublicKey(
        JwtBearerOptions options,
        ArchLucidAuthOptions authOptions,
        string configuredPemPath)
    {
        string resolvedPath = Path.IsPathRooted(configuredPemPath)
            ? configuredPemPath
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPemPath));

        if (!File.Exists(resolvedPath))
            throw new InvalidOperationException(
                $"ArchLucidAuth:JwtSigningPublicKeyPemPath points to a missing file: '{resolvedPath}'.");


        string pemText = File.ReadAllText(resolvedPath);
        RsaSecurityKey signingKey;

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(pemText);
            // Export parameters so validation does not hold a disposed RSA instance (using block ends before the host runs).
            signingKey = new RsaSecurityKey(rsa.ExportParameters(false));
        }

        string issuer = authOptions.JwtLocalIssuer.Trim();
        string audience = authOptions.JwtLocalAudience.Trim();

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            throw new InvalidOperationException(
                "ArchLucidAuth:JwtLocalIssuer and ArchLucidAuth:JwtLocalAudience are required when JwtSigningPublicKeyPemPath is set.");


        // MapInboundClaims=false keeps short JWT claim types ("name", "roles"). NameClaimType must use the same
        // string as the token payload. Options default is ClaimTypes.Name (long URI), which never matches inbound "name".
        string configuredNameClaimType = authOptions.NameClaimType.Trim();
        string nameClaimType =
            string.IsNullOrEmpty(configuredNameClaimType)
            || string.Equals(configuredNameClaimType, ClaimTypes.Name, StringComparison.Ordinal)
                ? "name"
                : configuredNameClaimType;

        options.RequireHttpsMetadata = false;
        // Keep JWT short claim names (e.g. "roles") so TokenValidationParameters.RoleClaimType matches Entra-style CI tokens.
        options.MapInboundClaims = false;
        options.Authority = string.Empty;
        options.MetadataAddress = string.Empty;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = "roles",
            NameClaimType = nameClaimType
        };
    }
}
