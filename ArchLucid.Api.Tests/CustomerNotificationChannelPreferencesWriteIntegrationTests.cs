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

/// <summary>Separate fixture from read tests so InMemory preferences store starts empty (singleton repo per host).</summary>
public sealed class CustomerNotificationChannelPreferencesWriteIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly JwtLocalSigningWebAppFactory _factory;

    public CustomerNotificationChannelPreferencesWriteIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Put_customer_channel_preferences_with_reader_jwt_returns_forbidden()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            issuer: "https://test.archlucid.local",
            audience: "api://archlucid-jwt-local-test",
            name: "ReaderUser",
            roles: [ArchLucidRoles.Reader]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TenantNotificationChannelPreferencesUpsertRequest body = new()
        {
            EmailCustomerNotificationsEnabled = false,
            TeamsCustomerNotificationsEnabled = true,
            OutboundWebhookCustomerNotificationsEnabled = false,
        };

        HttpResponseMessage res = await client.PutAsJsonAsync(
            new Uri("/v1/notifications/customer-channel-preferences", UriKind.Relative),
            body);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_customer_channel_preferences_with_operator_jwt_then_get_returns_configured_row()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            issuer: "https://test.archlucid.local",
            audience: "api://archlucid-jwt-local-test",
            name: "OperatorUser",
            roles: [ArchLucidRoles.Operator]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TenantNotificationChannelPreferencesUpsertRequest putBody = new()
        {
            EmailCustomerNotificationsEnabled = false,
            TeamsCustomerNotificationsEnabled = true,
            OutboundWebhookCustomerNotificationsEnabled = true,
        };

        HttpResponseMessage putRes = await client.PutAsJsonAsync(
            new Uri("/v1/notifications/customer-channel-preferences", UriKind.Relative),
            putBody);

        putRes.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantNotificationChannelPreferencesResponse? putParsed =
            await putRes.Content.ReadFromJsonAsync<TenantNotificationChannelPreferencesResponse>(JsonOptions);

        putParsed.Should().NotBeNull();
        putParsed.IsConfigured.Should().BeTrue();
        putParsed.EmailCustomerNotificationsEnabled.Should().BeFalse();
        putParsed.TeamsCustomerNotificationsEnabled.Should().BeTrue();
        putParsed.OutboundWebhookCustomerNotificationsEnabled.Should().BeTrue();

        HttpResponseMessage getRes = await client.GetAsync(new Uri("/v1/notifications/customer-channel-preferences", UriKind.Relative));

        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantNotificationChannelPreferencesResponse? getParsed =
            await getRes.Content.ReadFromJsonAsync<TenantNotificationChannelPreferencesResponse>(JsonOptions);

        getParsed.Should().NotBeNull();
        getParsed.IsConfigured.Should().BeTrue();
        getParsed.EmailCustomerNotificationsEnabled.Should().BeFalse();
        getParsed.TeamsCustomerNotificationsEnabled.Should().BeTrue();
        getParsed.OutboundWebhookCustomerNotificationsEnabled.Should().BeTrue();
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
        RSAParameters keyMaterial = rsa.ExportParameters(includePrivateParameters: true);
        RsaSecurityKey signingKey = new(keyMaterial);
        SigningCredentials creds = new(signingKey, SecurityAlgorithms.RsaSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, "test-sub"), new("name", name)];

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
