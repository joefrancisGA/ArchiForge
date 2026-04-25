using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Auth.Services;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests.Auth;

[Trait("Suite", "Core")]
public sealed class EntraMultiTenantJwtBearerConfiguratorTests
{
    [Fact]
    public void ApplyIfEnabled_when_multi_tenant_disabled_leaves_default_issuer_validation()
    {
        JwtBearerOptions options = new();
        ArchLucidAuthOptions auth = new() { MultiTenantEntra = false };

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, auth);

        options.TokenValidationParameters.IssuerValidator.Should().BeNull();
    }

    [Fact]
    public void ApplyIfEnabled_when_multi_tenant_enabled_validates_v2_issuer()
    {
        JwtBearerOptions options = new();
        ArchLucidAuthOptions auth = new() { MultiTenantEntra = true };

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, auth);

        string issuer = "https://login.microsoftonline.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0";
        string? validated = options.TokenValidationParameters.IssuerValidator!(
            issuer,
            new JwtSecurityToken(),
            new TokenValidationParameters());

        validated.Should().Be(issuer);
    }

    [Fact]
    public void ApplyIfEnabled_when_multi_tenant_enabled_rejects_non_entra_issuer()
    {
        JwtBearerOptions options = new();
        ArchLucidAuthOptions auth = new() { MultiTenantEntra = true };

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, auth);

        Func<string?> act = () => options.TokenValidationParameters.IssuerValidator!(
            "https://evil.example/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0",
            new JwtSecurityToken(),
            new TokenValidationParameters());

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public async Task ApplyIfEnabled_when_allowlist_configured_accepts_matching_tid()
    {
        Guid tid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        JwtBearerOptions options = new();
        ArchLucidAuthOptions auth = new() { MultiTenantEntra = true, AllowedEntraTenantIds = tid.ToString("D") };

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, auth);

        DefaultHttpContext http = new();
        AuthenticationScheme scheme = new(
            JwtBearerDefaults.AuthenticationScheme,
            "JWT Bearer",
            typeof(JwtBearerHandler));
        TokenValidatedContext ctx = new(http, scheme, options)
        {
            Principal = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("tid", tid.ToString("D"))]))
        };

        JwtBearerEvents events = options.Events ?? throw new InvalidOperationException("Events not wired.");
        await events.OnTokenValidated(ctx);

        ctx.Result?.Failure.Should().BeNull();
    }

    [Fact]
    public async Task ApplyIfEnabled_when_allowlist_configured_fails_without_tid()
    {
        Guid tid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        JwtBearerOptions options = new();
        ArchLucidAuthOptions auth = new() { MultiTenantEntra = true, AllowedEntraTenantIds = tid.ToString("D") };

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, auth);

        DefaultHttpContext http = new();
        AuthenticationScheme scheme = new(
            JwtBearerDefaults.AuthenticationScheme,
            "JWT Bearer",
            typeof(JwtBearerHandler));
        TokenValidatedContext ctx = new(http, scheme, options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity())
        };

        JwtBearerEvents events = options.Events ?? throw new InvalidOperationException("Events not wired.");
        await events.OnTokenValidated(ctx);

        ctx.Result?.Failure?.Message.Should().NotBeNull().And.ContainEquivalentOf("tid");
    }

    [Fact]
    public async Task ApplyIfEnabled_when_allowlist_configured_fails_for_unlisted_tid()
    {
        Guid allowed = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid wrong = Guid.Parse("11111111-1111-1111-1111-111111111111");
        JwtBearerOptions options = new();
        ArchLucidAuthOptions auth = new() { MultiTenantEntra = true, AllowedEntraTenantIds = allowed.ToString("D") };

        EntraMultiTenantJwtBearerConfigurator.ApplyIfEnabled(options, auth);

        DefaultHttpContext http = new();
        AuthenticationScheme scheme = new(
            JwtBearerDefaults.AuthenticationScheme,
            "JWT Bearer",
            typeof(JwtBearerHandler));
        TokenValidatedContext ctx = new(http, scheme, options)
        {
            Principal = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("tid", wrong.ToString("D"))]))
        };

        JwtBearerEvents events = options.Events ?? throw new InvalidOperationException("Events not wired.");
        await events.OnTokenValidated(ctx);

        ctx.Result?.Failure.Should().NotBeNull();
    }
}
