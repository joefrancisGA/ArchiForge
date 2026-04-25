using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Persistence.Billing.AzureMarketplace;

/// <summary>Validates Microsoft-issued JWTs for Marketplace SaaS webhooks using OIDC metadata.</summary>
public sealed class MicrosoftMarketplaceJwtVerifier(IOptionsMonitor<BillingOptions> billingOptions)
    : IMarketplaceWebhookTokenVerifier
{
    private readonly IOptionsMonitor<BillingOptions> _billingOptions =
        billingOptions ?? throw new ArgumentNullException(nameof(billingOptions));

    public async Task<ClaimsPrincipal?> ValidateAsync(
        string bearerToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return null;


        BillingOptions billing = _billingOptions.CurrentValue;
        string? metadataAddress = billing.AzureMarketplace.OpenIdMetadataAddress?.Trim();

        if (string.IsNullOrWhiteSpace(metadataAddress))
            return null;


        string metadataAddressRequired = metadataAddress;

        string[] audiences = billing.AzureMarketplace.ValidAudiences ?? [];

        if (audiences.Length == 0)
            return null;


        JwtSecurityTokenHandler handler = new();

        ConfigurationManager<OpenIdConnectConfiguration> configurationManager = new(
            metadataAddressRequired,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        OpenIdConnectConfiguration configuration = await configurationManager
            .GetConfigurationAsync(cancellationToken)
            .ConfigureAwait(false);

        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuers = configuration.Issuer is not null ? [configuration.Issuer] : null,
            ValidateAudience = true,
            ValidAudiences = audiences,
            ValidateLifetime = true,
            IssuerSigningKeys = configuration.SigningKeys
        };

        try
        {
            return handler.ValidateToken(bearerToken, validationParameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
