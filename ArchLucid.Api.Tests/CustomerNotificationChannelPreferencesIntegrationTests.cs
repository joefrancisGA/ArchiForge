using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using ArchLucid.Contracts.Notifications;
using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests;

/// <summary>
///     JWT + in-memory storage: <see cref="CustomerNotificationChannelPreferencesController" /> returns defaults when
///     no SQL row.
/// </summary>
public sealed class CustomerNotificationChannelPreferencesIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JwtLocalSigningWebAppFactory _factory;

    public CustomerNotificationChannelPreferencesIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Get_customer_channel_preferences_with_reader_jwt_returns_unconfigured_defaults()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "ReaderUser",
            [ArchLucidRoles.Reader]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage res =
            await client.GetAsync(new Uri("/v1/notifications/customer-channel-preferences", UriKind.Relative));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantNotificationChannelPreferencesResponse? body =
            await res.Content.ReadFromJsonAsync<TenantNotificationChannelPreferencesResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.IsConfigured.Should().BeFalse();
        body.EmailCustomerNotificationsEnabled.Should().BeTrue();
        body.TeamsCustomerNotificationsEnabled.Should().BeFalse();
        body.OutboundWebhookCustomerNotificationsEnabled.Should().BeFalse();
        body.SchemaVersion.Should().Be(1);
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
        claims.AddRange(roles.Select(r => new Claim("roles", r)));

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
