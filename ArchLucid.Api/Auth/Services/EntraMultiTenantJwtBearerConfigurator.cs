using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;

using ArchLucid.Api.Auth.Models;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Auth.Services;

/// <summary>Multi-organization Entra ID: issuer validation + optional <c>tid</c> allowlist.</summary>
internal static class EntraMultiTenantJwtBearerConfigurator
{
    private static readonly Regex AzureAdIssuerV2 = new(
        @"^https://login\.microsoftonline\.com/[0-9a-fA-F-]{36}/v2\.0/?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void ApplyIfEnabled(JwtBearerOptions options, ArchLucidAuthOptions auth)
    {
        if (!auth.MultiTenantEntra)
            return;


        options.TokenValidationParameters.IssuerValidator = ValidateIssuer;

        IReadOnlyList<Guid> allowList = ParseAllowedEntraTenantIds(auth.AllowedEntraTenantIds);

        if (allowList.Count == 0)
            return;


        JwtBearerEvents prior = options.Events ?? new JwtBearerEvents();

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = prior.OnAuthenticationFailed,
            OnChallenge = prior.OnChallenge,
            OnForbidden = prior.OnForbidden,
            OnMessageReceived = prior.OnMessageReceived,
            OnTokenValidated = async ctx =>
            {
                if (prior.OnTokenValidated is not null)
                    await prior.OnTokenValidated(ctx).ConfigureAwait(false);


                if (!TryGetTenantId(ctx.Principal, out Guid tid))
                {
                    ctx.Fail("JWT is missing tid claim.");

                    return;
                }

                if (allowList.All(g => g != tid))

                    ctx.Fail("Entra tenant id is not listed in ArchLucidAuth:AllowedEntraTenantIds.");

            },
        };
    }

    private static string? ValidateIssuer(string? issuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
    {
        _ = securityToken;
        _ = validationParameters;

        if (string.IsNullOrWhiteSpace(issuer))
            throw new SecurityTokenInvalidIssuerException("Issuer is missing.");


        string trimmed = issuer.Trim();

        if (!AzureAdIssuerV2.IsMatch(trimmed))
            throw new SecurityTokenInvalidIssuerException("Issuer is not a valid Azure AD v2.0 issuer.");


        return trimmed;
    }

    private static IReadOnlyList<Guid> ParseAllowedEntraTenantIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];


        List<Guid> list = [];

        foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))

            if (Guid.TryParse(part, out Guid g))

                list.Add(g);



        return list;
    }

    private static bool TryGetTenantId(ClaimsPrincipal? principal, out Guid tenantId)
    {
        tenantId = Guid.Empty;

        string? tid = principal?.FindFirst("tid")?.Value;

        if (string.IsNullOrWhiteSpace(tid))
            return false;


        return Guid.TryParse(tid, CultureInfo.InvariantCulture, out tenantId);
    }
}
