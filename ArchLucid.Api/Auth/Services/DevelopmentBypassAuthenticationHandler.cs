using System.Security.Claims;
using System.Text.Encodings.Web;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Core.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Auth.Services;

public sealed class DevelopmentBypassAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ArchLucidAuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentBypass";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ArchLucidAuthOptions opts = authOptions.Value;
        string role = string.IsNullOrWhiteSpace(opts.DevRole) ? ArchLucidRoles.Admin : opts.DevRole.Trim();

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, opts.DevUserId),
            new(ClaimTypes.Name, opts.DevUserName),
            new(ClaimTypes.Role, role)
        ];

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
