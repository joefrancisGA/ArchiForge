using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests;

/// <summary>JWT validation using <see cref="ArchLucidAuthOptions.JwtSigningPublicKeyPemPath" /> (CI / local E2E pattern).</summary>
public sealed class JwtLocalSigningIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public JwtLocalSigningIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_architecture_runs_with_valid_local_jwt_returns_OK()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "JwtTestUser",
            [ArchLucidRoles.Admin]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage res = await client.GetAsync(new Uri("/v1/architecture/runs", UriKind.Relative));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_architecture_runs_with_malformed_bearer_returns_Unauthorized()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        HttpResponseMessage res = await client.GetAsync(new Uri("/v1/architecture/runs", UriKind.Relative));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string MintJwt(
        string privatePkcs8Pem,
        string issuer,
        string audience,
        string name,
        IReadOnlyList<string> roles)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportFromPem(privatePkcs8Pem);
        RSAParameters keyMaterial = rsa.ExportParameters(true);
        RsaSecurityKey signingKey = new(keyMaterial);
        SigningCredentials creds = new(signingKey, SecurityAlgorithms.RsaSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, "test-sub"), new("name", name)];

        foreach (string r in roles)
        {
            claims.Add(new Claim("roles", r));
        }

        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = new(
            issuer,
            audience,
            claims,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddHours(1),
            creds);

        return handler.WriteToken(token);
    }
}
