using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
///     In-memory API host with <c>ArchLucidAuth:Mode=JwtBearer</c> and local RSA validation
///     (<c>ArchLucidAuth:JwtSigningPublicKeyPemPath</c>) — mirrors CI live E2E.
/// </summary>
public sealed class JwtLocalSigningWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _publicPemPath;

    public JwtLocalSigningWebAppFactory()
    {
        using RSA rsa = RSA.Create(2048);
        PrivatePemForTests = rsa.ExportPkcs8PrivateKeyPem();
        string publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        _publicPemPath = Path.Combine(Path.GetTempPath(), $"archlucid-jwt-local-{Guid.NewGuid():N}.pem");
        File.WriteAllText(_publicPemPath, publicPem, Encoding.UTF8);
    }

    /// <summary>PKCS#8 private key PEM used to mint JWTs in tests (never used by the API host).</summary>
    public string PrivatePemForTests
    {
        get;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting merges into host config early so Program's AddArchLucidAuth sees JwtBearer (not appsettings.json DevelopmentBypass).
        builder.UseSetting("ArchLucidAuth:Mode", "JwtBearer");
        builder.UseSetting("ArchLucidAuth:Authority", "");
        builder.UseSetting("ArchLucidAuth:Audience", "");
        builder.UseSetting("ArchLucidAuth:JwtSigningPublicKeyPemPath", _publicPemPath);
        builder.UseSetting("ArchLucidAuth:JwtLocalIssuer", "https://test.archlucid.local");
        builder.UseSetting("ArchLucidAuth:JwtLocalAudience", "api://archlucid-jwt-local-test");
        builder.UseSetting("Authentication:ApiKey:DevelopmentBypassAll", "false");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchLucid:StorageProvider"] = "InMemory",
                    ["ConnectionStrings:ArchLucid"] = "",
                    ["AgentExecution:Mode"] = "Simulator",
                    ["AzureOpenAI:Endpoint"] = "",
                    ["AzureOpenAI:ApiKey"] = "",
                    ["AzureOpenAI:DeploymentName"] = "",
                    ["AzureOpenAI:EmbeddingDeploymentName"] = "",
                    ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
                    ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
                    ["RateLimiting:Expensive:PermitLimit"] = "100000",
                    ["RateLimiting:Expensive:WindowMinutes"] = "1",
                    ["RateLimiting:Replay:Light:PermitLimit"] = "100000",
                    ["RateLimiting:Replay:Heavy:PermitLimit"] = "100000",
                    ["ArchLucidAuth:Mode"] = "JwtBearer",
                    ["ArchLucidAuth:Authority"] = "",
                    ["ArchLucidAuth:Audience"] = "",
                    ["ArchLucidAuth:JwtSigningPublicKeyPemPath"] = _publicPemPath,
                    ["ArchLucidAuth:JwtLocalIssuer"] = "https://test.archlucid.local",
                    ["ArchLucidAuth:JwtLocalAudience"] = "api://archlucid-jwt-local-test",
                    ["Authentication:ApiKey:DevelopmentBypassAll"] = "false",
                    ["Billing:Provider"] = "Noop"
                });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            if (File.Exists(_publicPemPath))
            {
                File.Delete(_publicPemPath);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
