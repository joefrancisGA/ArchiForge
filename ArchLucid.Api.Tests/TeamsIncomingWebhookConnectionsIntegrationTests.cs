using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using ArchLucid.Api.Routing;
using ArchLucid.Contracts.Integrations;
using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests;

public sealed class TeamsIncomingWebhookConnectionsIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JwtLocalSigningWebAppFactory _factory;

    public TeamsIncomingWebhookConnectionsIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_connections_with_reader_jwt_returns_forbidden()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "ReaderUser",
            [ArchLucidRoles.Reader]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TeamsIncomingWebhookConnectionUpsertRequest body = new() { KeyVaultSecretName = "teams-incoming-webhook-demo" };

        HttpResponseMessage res = await client.PostAsJsonAsync(
            new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative),
            body);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_connections_with_https_body_returns_bad_request()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "OperatorUser",
            [ArchLucidRoles.Operator]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TeamsIncomingWebhookConnectionUpsertRequest body = new()
        {
            KeyVaultSecretName = "https://example.invalid/hook"
        };

        HttpResponseMessage res = await client.PostAsJsonAsync(
            new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative),
            body);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_post_delete_round_trip_with_operator_jwt()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "OperatorUser",
            [ArchLucidRoles.Operator]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage get0 =
            await client.GetAsync(new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative));
        get0.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamsIncomingWebhookConnectionResponse? parsed0 =
            await get0.Content.ReadFromJsonAsync<TeamsIncomingWebhookConnectionResponse>(JsonOptions);
        parsed0.Should().NotBeNull();
        parsed0!.IsConfigured.Should().BeFalse();
        parsed0.EnabledTriggers.Should().Contain("com.archlucid.authority.run.completed");
        parsed0.EnabledTriggers.Should().Contain("com.archlucid.seat.reservation.released");
        parsed0.EnabledTriggers.Should().HaveCount(6, "fresh tenants default to the v1 all-on catalog");

        TeamsIncomingWebhookConnectionUpsertRequest putBody = new()
        {
            KeyVaultSecretName = "kv-teams-webhook-ref",
            Label = "demo tenant — replace before publishing",
            EnabledTriggers =
            [
                "com.archlucid.authority.run.completed",
                "com.archlucid.alert.fired"
            ]
        };

        HttpResponseMessage post = await client.PostAsJsonAsync(
            new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative),
            putBody);
        post.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamsIncomingWebhookConnectionResponse? postParsed =
            await post.Content.ReadFromJsonAsync<TeamsIncomingWebhookConnectionResponse>(JsonOptions);
        postParsed.Should().NotBeNull();
        postParsed!.IsConfigured.Should().BeTrue();
        postParsed.KeyVaultSecretName.Should().Be("kv-teams-webhook-ref");
        postParsed.EnabledTriggers.Should()
            .BeEquivalentTo("com.archlucid.authority.run.completed", "com.archlucid.alert.fired");

        HttpResponseMessage get1 =
            await client.GetAsync(new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative));
        get1.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage del = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete,
                new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative)));
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_connections_with_unknown_trigger_returns_bad_request()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "OperatorUser",
            [ArchLucidRoles.Operator]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TeamsIncomingWebhookConnectionUpsertRequest body = new()
        {
            KeyVaultSecretName = "kv-teams-webhook-ref",
            EnabledTriggers = ["com.archlucid.authority.run.completed", "com.archlucid.does.not.exist"]
        };

        HttpResponseMessage res = await client.PostAsJsonAsync(
            new Uri($"/{ApiV1Routes.TeamsIncomingWebhookConnections}", UriKind.Relative),
            body);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string text = await res.Content.ReadAsStringAsync();
        text.Should().Contain("com.archlucid.does.not.exist");
    }

    [Fact]
    public async Task Get_triggers_catalog_returns_v1_default_set()
    {
        string token = MintJwt(
            _factory.PrivatePemForTests,
            "https://test.archlucid.local",
            "api://archlucid-jwt-local-test",
            "ReaderUser",
            [ArchLucidRoles.Reader]);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage res = await client.GetAsync(
            new Uri($"/{ApiV1Routes.TeamsNotificationTriggerCatalog}", UriKind.Relative));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        string[]? triggers = await res.Content.ReadFromJsonAsync<string[]>(JsonOptions);
        triggers.Should().NotBeNull();
        triggers!.Should().Contain("com.archlucid.compliance.drift.escalated");
        triggers!.Should().Contain("com.archlucid.advisory.scan.completed");
        triggers!.Should().Contain("com.archlucid.seat.reservation.released");
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
