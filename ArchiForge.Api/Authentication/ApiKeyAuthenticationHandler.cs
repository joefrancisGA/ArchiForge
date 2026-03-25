using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ArchiForge.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        bool enabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);

        // If API key auth is disabled, treat all requests as authenticated so existing callers/tests keep working.
        if (!enabled)
        {
            // When disabled, include full permissions so policy-protected endpoints continue to work locally.
            ClaimsIdentity identity = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, "DevUser"),
                new Claim("permission", "commit:run"),
                new Claim("permission", "seed:results"),
                new Claim("permission", "export:consulting-docx"),
                new Claim("permission", "metrics:read"),
                new Claim("permission", "replay:comparisons"),
                new Claim("permission", "replay:diagnostics")
            ], Scheme.Name);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        if (!Request.Headers.TryGetValue("X-Api-Key", out StringValues providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key header 'X-Api-Key' is missing."));
        }

        string key = providedKey.ToString();

        string? adminKey = configuration["Authentication:ApiKey:AdminKey"];
        string? readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        string? userName;
        Claim[] claims;

        if (!string.IsNullOrWhiteSpace(adminKey) && string.Equals(key, adminKey, StringComparison.Ordinal))
        {
            userName = "ApiKeyAdmin";
            claims =
            [
                new Claim(ClaimTypes.Name, userName),
                new Claim("permission", "commit:run"),
                new Claim("permission", "seed:results"),
                new Claim("permission", "export:consulting-docx"),
                new Claim("permission", "metrics:read"),
                new Claim("permission", "replay:comparisons"),
                new Claim("permission", "replay:diagnostics")
            ];
        }
        else if (!string.IsNullOrWhiteSpace(readerKey) && string.Equals(key, readerKey, StringComparison.Ordinal))
        {
            userName = "ApiKeyReadOnly";
            claims =
            [
                new Claim(ClaimTypes.Name, userName),
                new Claim("permission", "metrics:read")
            ];
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        ClaimsIdentity successIdentity = new ClaimsIdentity(claims, Scheme.Name);
        ClaimsPrincipal successPrincipal = new ClaimsPrincipal(successIdentity);
        AuthenticationTicket successTicket = new AuthenticationTicket(successPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(successTicket));
    }
}

