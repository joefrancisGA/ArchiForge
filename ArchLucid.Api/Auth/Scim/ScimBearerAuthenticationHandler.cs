using System.Security.Claims;
using System.Text.Encodings.Web;

using ArchLucid.Application.Scim.Tokens;
using ArchLucid.Core.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Auth.Scim;

public sealed class ScimBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IScimBearerTokenAuthenticator authenticator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IScimBearerTokenAuthenticator _authenticator =
        authenticator ?? throw new ArgumentNullException(nameof(authenticator));

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues header))
            return AuthenticateResult.Fail("Missing Authorization header.");

        string raw = header.ToString();

        if (!raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Authorization must use Bearer scheme.");

        string token = raw["Bearer ".Length..].Trim();

        ScimBearerAuthenticationResult? auth =
            await _authenticator.TryAuthenticateAsync(token, Context.RequestAborted);

        if (auth is null)
            return AuthenticateResult.Fail("Invalid or revoked SCIM token.");

        Claim[] claims =
        [
            new("tenant_id", auth.TenantId.ToString("D")),
            new("scim_token_id", auth.TokenRowId.ToString("D"))
        ];

        ClaimsIdentity identity = new(claims, ScimBearerDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, ScimBearerDefaults.AuthenticationScheme));
    }
}
