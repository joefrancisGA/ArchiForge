using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using ArchLucid.Api.Auth.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ArchLucid.Api.Authentication;

/// <summary>
/// API key authentication. When <c>Authentication:ApiKey:Enabled</c> is false, authentication fails closed
/// unless <c>Authentication:ApiKey:DevelopmentBypassAll</c> is true in a non-Production environment
/// (explicit opt-in for local development only; blocked in Production by API startup and <see cref="ArchLucid.Host.Core.Startup.Validation.ArchLucidConfigurationRules"/>).
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        bool enabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);
        bool developmentBypassAll = configuration.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false);

        // Previously, Enabled=false authenticated every request as a synthetic admin-equivalent principal. That meant
        // any host misconfigured with ArchLucidAuth:Mode=ApiKey but keys "off" silently granted full access to anonymous callers.
        if (!enabled)
        {
            if (!developmentBypassAll)
            {
                return Task.FromResult(
                    AuthenticateResult.Fail(
                        "API key authentication is disabled. Set Authentication:ApiKey:Enabled to true and configure keys, " +
                        "or set Authentication:ApiKey:DevelopmentBypassAll=true only in non-Production for intentional open access."));
            }

            if (environment.IsProduction())
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Authentication:ApiKey:DevelopmentBypassAll is not allowed in Production."));
            }

            ClaimsIdentity bypassIdentity = new(
                BuildSyntheticAdminClaims(),
                Scheme.Name);
            ClaimsPrincipal bypassPrincipal = new(bypassIdentity);
            AuthenticationTicket bypassTicket = new(bypassPrincipal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(bypassTicket));
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

        if (!string.IsNullOrWhiteSpace(adminKey) && ConstantTimeKeyEquals(key, adminKey))
        {
            userName = "ApiKeyAdmin";
            claims =
            [
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, ArchLucidRoles.Admin),
                new Claim("permission", "commit:run"),
                new Claim("permission", "seed:results"),
                new Claim("permission", "export:consulting-docx"),
                new Claim("permission", "metrics:read"),
                new Claim("permission", "replay:comparisons"),
                new Claim("permission", "replay:diagnostics")
            ];
        }
        else if (!string.IsNullOrWhiteSpace(readerKey) && ConstantTimeKeyEquals(key, readerKey))
        {
            userName = "ApiKeyReadOnly";
            claims =
            [
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, ArchLucidRoles.Reader),
                new Claim("permission", "metrics:read")
            ];
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        ClaimsIdentity successIdentity = new(claims, Scheme.Name);
        ClaimsPrincipal successPrincipal = new(successIdentity);
        AuthenticationTicket successTicket = new(successPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(successTicket));
    }

    /// <summary>
    /// Compares UTF-8 key material using SHA-256 digests so length and timing do not leak raw key bytes.
    /// </summary>
    private static bool ConstantTimeKeyEquals(string provided, string expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        ReadOnlySpan<byte> a = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        ReadOnlySpan<byte> b = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static Claim[] BuildSyntheticAdminClaims()
    {
        return
        [
            new Claim(ClaimTypes.Name, "DevUser"),
            new Claim(ClaimTypes.Role, ArchLucidRoles.Admin),
            new Claim("permission", "commit:run"),
            new Claim("permission", "seed:results"),
            new Claim("permission", "export:consulting-docx"),
            new Claim("permission", "metrics:read"),
            new Claim("permission", "replay:comparisons"),
            new Claim("permission", "replay:diagnostics")
        ];
    }
}
