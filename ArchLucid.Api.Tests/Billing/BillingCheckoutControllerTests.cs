using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using ArchLucid.Api.Models.Billing;
using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests.Billing;

[Trait("Suite", "Core")]
public sealed class BillingCheckoutControllerTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public BillingCheckoutControllerTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Checkout_without_bearer_returns_401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1/tenant/billing/checkout",
            new BillingCheckoutPostRequest
            {
                TargetTier = "Team",
                Seats = 1,
                Workspaces = 1,
                ReturnUrl = "https://app.example.com/ok",
                CancelUrl = "https://app.example.com/cancel"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_with_reader_jwt_returns_403()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "ReaderUser",
            [ArchLucidRoles.Reader]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1/tenant/billing/checkout",
            new BillingCheckoutPostRequest
            {
                TargetTier = "Team",
                Seats = 1,
                Workspaces = 1,
                ReturnUrl = "https://app.example.com/ok",
                CancelUrl = "https://app.example.com/cancel"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Checkout_with_admin_jwt_returns_200()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "AdminUser",
            [ArchLucidRoles.Admin]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1/tenant/billing/checkout",
            new BillingCheckoutPostRequest
            {
                TargetTier = "Team",
                Seats = 2,
                Workspaces = 1,
                BillingEmail = "billing@example.com",
                ReturnUrl = "https://app.example.com/ok",
                CancelUrl = "https://app.example.com/cancel"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        BillingCheckoutResponseDto? dto =
            await response.Content.ReadFromJsonAsync<BillingCheckoutResponseDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        dto.Should().NotBeNull();
        dto.CheckoutUrl.Should().NotBeNullOrWhiteSpace();
        dto.ProviderSessionId.Should().NotBeNullOrWhiteSpace();
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
