using System.Security.Claims;
using System.Text.Encodings.Web;

using ArchiForge.Api.Auth.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Auth.Services;

public sealed class DevelopmentBypassAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ArchiForgeAuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentBypass";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ArchiForgeAuthOptions opts = authOptions.Value;
        string role = string.IsNullOrWhiteSpace(opts.DevRole) ? ArchiForgeRoles.Admin : opts.DevRole.Trim();

        List<Claim> claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, opts.DevUserId),
            new(ClaimTypes.Name, opts.DevUserName),
            new(ClaimTypes.Role, role)
        };

        ClaimsIdentity identity = new ClaimsIdentity(claims, SchemeName);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        AuthenticationTicket ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
