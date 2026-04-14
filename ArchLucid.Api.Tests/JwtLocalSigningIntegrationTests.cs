using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests;

/// <summary>JWT validation using <see cref="ArchLucidAuthOptions.JwtSigningPublicKeyPemPath"/> (CI / local E2E pattern).</summary>
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
            issuer: "https://test.archlucid.local",
            audience: "api://archlucid-jwt-local-test",
            name: "JwtTestUser",
            roles: [ArchLucidRoles.Admin]);

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
        SigningCredentials creds = new(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        List<Claim> claims = [new Claim(JwtRegisteredClaimNames.Sub, "test-sub"), new Claim("name", name)];

        foreach (string r in roles)
        {
            claims.Add(new Claim("roles", r));
        }

        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return handler.WriteToken(token);
    }
}
