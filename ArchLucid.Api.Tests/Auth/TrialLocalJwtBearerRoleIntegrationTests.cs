using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using ArchLucid.Api.Auth.Services;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests.Auth;

/// <summary>
///     JWT minted like trial local identity (same issuer/audience/signing key as
///     <see cref="JwtLocalSigningWebAppFactory" />)
///     must satisfy <see cref="ArchLucidPolicies.ReadAuthority" /> for Reader and fail
///     <see cref="ArchLucidPolicies.ExecuteAuthority" /> until Operator/Admin.
/// </summary>
[Trait("Suite", "Api")]
[Trait("Category", "Integration")]
public sealed class TrialLocalJwtBearerRoleIntegrationTests
{
    [SkippableFact]
    public async Task Reader_jwt_allows_read_authority_jobs_route()
    {
        await using TrialJwtHost host = await TrialJwtHost.CreateAsync();
        host.SetBearer(host.MintJwt(ArchLucidRoles.Reader));

        using HttpResponseMessage response =
            await host.Client.GetAsync("/v1/jobs/00000000-0000-0000-0000-000000000001", CancellationToken.None);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SkippableFact]
    public async Task Reader_jwt_forbidden_on_execute_authority_create_run()
    {
        await using TrialJwtHost host = await TrialJwtHost.CreateAsync();
        host.SetBearer(host.MintJwt(ArchLucidRoles.Reader));

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/architecture/request");
        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    requestId = $"trial-jwt-reader-{Guid.NewGuid():N}",
                    description = "Trial JWT reader role gate".PadRight(80, ' '),
                    systemName = "TrialJwtGate",
                    environment = "prod",
                    cloudProvider = 1,
                    constraints = Array.Empty<string>(),
                    requiredCapabilities = new[] { "SQL" },
                    assumptions = Array.Empty<string>(),
                    priorManifestVersion = (string?)null
                }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await host.Client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SkippableFact]
    public async Task Admin_jwt_passes_execute_authority_role_gate_for_create_run()
    {
        await using TrialJwtHost host = await TrialJwtHost.CreateAsync();
        host.SetBearer(host.MintJwt(ArchLucidRoles.Admin));

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/architecture/request");
        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    requestId = $"trial-jwt-admin-{Guid.NewGuid():N}",
                    description = "Trial JWT admin role gate".PadRight(80, ' '),
                    systemName = "TrialJwtGate",
                    environment = "prod",
                    cloudProvider = 1,
                    constraints = Array.Empty<string>(),
                    requiredCapabilities = new[] { "SQL" },
                    assumptions = Array.Empty<string>(),
                    priorManifestVersion = (string?)null
                }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await host.Client.SendAsync(request, CancellationToken.None);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class TrialJwtHost : IAsyncDisposable
    {
        private readonly string _privateKeyPath;

        private TrialJwtHost(string privateKeyPath, WebApplicationFactory<Program> app, HttpClient client)
        {
            _privateKeyPath = privateKeyPath;
            App = app;
            Client = client;
        }

        private WebApplicationFactory<Program> App
        {
            get;
        }

        public HttpClient Client
        {
            get;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();

            try
            {
                if (File.Exists(_privateKeyPath))
                {
                    File.Delete(_privateKeyPath);
                }
            }
            catch
            {
                // best-effort
            }
        }

        public static async Task<TrialJwtHost> CreateAsync()
        {
            JwtLocalSigningWebAppFactory inner = new();
            string privateKeyPath =
                Path.Combine(Path.GetTempPath(), $"archlucid-trial-local-jwt-{Guid.NewGuid():N}.pem");
            await File.WriteAllTextAsync(privateKeyPath, inner.PrivatePemForTests);

            WebApplicationFactory<Program> app = inner.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Auth:Trial:Modes:0"] = "LocalIdentity",
                            ["Auth:Trial:LocalIdentity:JwtPrivateKeyPemPath"] = privateKeyPath,
                            ["Auth:Trial:LocalIdentity:JwtIssuer"] = "https://test.archlucid.local",
                            ["Auth:Trial:LocalIdentity:JwtAudience"] = "api://archlucid-jwt-local-test"
                        });
                });
            });

            HttpClient client = app.CreateClient();

            return new TrialJwtHost(privateKeyPath, app, client);
        }

        public void SetBearer(string jwt)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        public string MintJwt(string role)
        {
            using IServiceScope scope = App.Services.CreateScope();
            ILocalTrialJwtIssuer issuer = scope.ServiceProvider.GetRequiredService<ILocalTrialJwtIssuer>();

            return issuer.IssueAccessToken(
                Guid.NewGuid(),
                $"{role.ToLowerInvariant()}@trial-jwt.test",
                role,
                ScopeIds.DefaultTenant,
                ScopeIds.DefaultWorkspace,
                ScopeIds.DefaultProject);
        }
    }
}
