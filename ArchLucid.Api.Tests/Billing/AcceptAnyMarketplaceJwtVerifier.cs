using System.Security.Claims;

using ArchLucid.Core.Billing;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>Test double: accepts any non-empty bearer string so Marketplace webhook tests do not call Microsoft OIDC.</summary>
internal sealed class AcceptAnyMarketplaceJwtVerifier : IMarketplaceWebhookTokenVerifier
{
    public Task<ClaimsPrincipal?> ValidateAsync(string bearerToken, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(bearerToken)
            ? Task.FromResult<ClaimsPrincipal?>(null)
            : Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
