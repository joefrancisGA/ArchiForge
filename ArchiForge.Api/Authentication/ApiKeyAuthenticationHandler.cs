using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

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
        var enabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);

        // If API key auth is disabled, treat all requests as authenticated so existing callers/tests keep working.
        if (!enabled)
        {
            // When disabled, include full permissions so policy-protected endpoints continue to work locally.
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, "DevUser"),
                new Claim("permission", "commit:run"),
                new Claim("permission", "seed:results"),
                new Claim("permission", "export:consulting-docx"),
                new Claim("permission", "metrics:read"),
                new Claim("permission", "replay:comparisons"),
                new Claim("permission", "replay:diagnostics")
            ], Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        if (!Request.Headers.TryGetValue("X-Api-Key", out var providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key header 'X-Api-Key' is missing."));
        }

        var key = providedKey.ToString();

        var adminKey = configuration["Authentication:ApiKey:AdminKey"];
        var readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

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

        var successIdentity = new ClaimsIdentity(claims, Scheme.Name);
        var successPrincipal = new ClaimsPrincipal(successIdentity);
        var successTicket = new AuthenticationTicket(successPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(successTicket));
    }
}

