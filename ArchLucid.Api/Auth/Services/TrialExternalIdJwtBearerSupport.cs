using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Auth.Services;

internal static class TrialExternalIdJwtBearerSupport
{
    /// <summary>
    /// When trial External ID is enabled, extends an existing strict issuer validator (for example multi-tenant Entra)
    /// to also accept Entra External ID (CIAM) issuers.
    /// </summary>
    public static void TryAllowConsumerIdentityIssuers(JwtBearerOptions options, bool trialExternalIdEnabled)
    {
        if (!trialExternalIdEnabled)
            return;

        IssuerValidator? prior = options.TokenValidationParameters.IssuerValidator;

        if (prior is null)
            return;

        options.TokenValidationParameters.IssuerValidator = (issuer, securityToken, validationParameters) => ExternalIdIssuerPatterns.IsConsumerIdentityIssuer(issuer) ? issuer! : prior(issuer, securityToken, validationParameters);
    }
}
