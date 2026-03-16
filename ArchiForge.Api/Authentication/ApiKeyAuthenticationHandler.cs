using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var enabled = _configuration.GetValue("Authentication:ApiKey:Enabled", false);

        // If API key auth is disabled, treat all requests as authenticated so existing callers/tests keep working.
        if (!enabled)
        {
            var identity = new ClaimsIdentity(Array.Empty<Claim>(), Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        if (!Request.Headers.TryGetValue("X-Api-Key", out var providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key header 'X-Api-Key' is missing."));
        }

        var expectedKey = _configuration["Authentication:ApiKey:Key"];
        if (string.IsNullOrWhiteSpace(expectedKey) ||
            !string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser")
        };
        var successIdentity = new ClaimsIdentity(claims, Scheme.Name);
        var successPrincipal = new ClaimsPrincipal(successIdentity);
        var successTicket = new AuthenticationTicket(successPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(successTicket));
    }
}

