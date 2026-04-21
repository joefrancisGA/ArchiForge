using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Value;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ArchLucid.Api.Tests.ValueReports;

public sealed class ValueReportControllerIntegrationTests : IAsyncLifetime
{
    private readonly JwtLocalSigningWebAppFactory _baseFactory = new();
    private readonly WebApplicationFactory<Program> _factory;

    public ValueReportControllerIntegrationTests()
    {
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(
                (_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ValueReport:Computation:AsyncJobWhenWindowDaysExceeds"] = "5000",
                    }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IValueReportMetricsReader>();
                services.AddSingleton<IValueReportMetricsReader, StubValueReportMetricsReader>();
            });
        });
    }

    public async Task InitializeAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();

        if (await tenants.GetByIdAsync(ScopeIds.DefaultTenant, CancellationToken.None) is not null)
            return;

        await tenants.InsertTenantAsync(
            ScopeIds.DefaultTenant,
            "Value report test tenant",
            "valuereporttest",
            TenantTier.Standard,
            null,
            CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        _baseFactory.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Post_generate_returns_docx_when_operator_jwt_and_standard_tier()
    {
        string token = MintJwt(
            _baseFactory.PrivatePemForTests,
            issuer: "https://test.archlucid.local",
            audience: "api://archlucid-jwt-local-test",
            name: "OperatorUser",
            roles: [ArchLucidRoles.Operator],
            tenantId: ScopeIds.DefaultTenant,
            workspaceId: ScopeIds.DefaultWorkspace,
            projectId: ScopeIds.DefaultProject);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Uri url = new(
            $"/v1.0/value-report/{ScopeIds.DefaultTenant:D}/generate?from=2026-01-01T00:00:00.0000000Z&to=2026-01-10T00:00:00.0000000Z",
            UriKind.Relative);

        using HttpResponseMessage res = await client.PostAsync(url, content: null);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        byte[] body = await res.Content.ReadAsByteArrayAsync();
        body.Should().NotBeEmpty();
    }

    private static string MintJwt(
        string privatePkcs8Pem,
        string issuer,
        string audience,
        string name,
        IReadOnlyList<string> roles,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(privatePkcs8Pem);
        RSAParameters keyMaterial = rsa.ExportParameters(includePrivateParameters: true);
        RsaSecurityKey signingKey = new(keyMaterial);
        SigningCredentials creds = new(signingKey, SecurityAlgorithms.RsaSha256);

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, "test-sub"),
            new Claim("name", name),
            new Claim("tenant_id", tenantId.ToString("D")),
            new Claim("workspace_id", workspaceId.ToString("D")),
            new Claim("project_id", projectId.ToString("D")),
        ];

        foreach (string r in roles)
            claims.Add(new Claim("roles", r));

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

    private sealed class StubValueReportMetricsReader : IValueReportMetricsReader
    {
        public Task<ValueReportRawMetrics> ReadAsync(
            Guid tenantId,
            Guid workspaceId,
            Guid projectId,
            DateTimeOffset fromUtcInclusive,
            DateTimeOffset toUtcExclusive,
            CancellationToken cancellationToken)
        {
            _ = tenantId;
            _ = workspaceId;
            _ = projectId;
            _ = fromUtcInclusive;
            _ = toUtcExclusive;
            _ = cancellationToken;

            ValueReportRawMetrics raw = new(
                [new ValueReportRunStatusCount("Completed", 1)],
                RunsCompletedCount: 1,
                ManifestsCommittedCount: 1,
                GovernanceEventCount: 1,
                DriftAlertEventCount: 1,
                TenantBaselineReviewCycleHours: null,
                TenantBaselineReviewCycleSource: null,
                TenantBaselineReviewCycleCapturedUtc: null,
                MeasuredAverageReviewCycleHoursForWindow: null,
                MeasuredReviewCycleSampleSize: 0);

            return Task.FromResult(raw);
        }
    }
}
