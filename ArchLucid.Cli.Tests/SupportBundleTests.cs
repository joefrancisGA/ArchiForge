using System.Net;
using System.Text;

using ArchLucid.Cli.Support;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SupportBundleTests
{
    [Fact]
    public void RedactHttpUrl_strips_user_info()
    {
        SupportBundleRedactor.RedactHttpUrl("https://user:secret@api.example.com:8443/v1")
            .Should()
            .Be("https://api.example.com:8443/v1");
    }

    [Fact]
    public void IsSensitiveEnvironmentVariableName_detects_common_secret_patterns()
    {
        SupportBundleRedactor.IsSensitiveEnvironmentVariableName("ARCHLUCID_API_KEY").Should().BeTrue();
        SupportBundleRedactor.IsSensitiveEnvironmentVariableName("ARCHLUCID_SOME_PASSWORD").Should().BeTrue();
        SupportBundleRedactor.IsSensitiveEnvironmentVariableName("DOTNET_ROOT").Should().BeFalse();
    }

    [Fact]
    public void RedactSensitivePatterns_strips_bearer_api_key_and_connection_secrets()
    {
        const string raw = """
                           {"h":"Authorization: Bearer supersecret","x":"X-Api-Key: abcdef","c":"Server=x;Password=hunter2;AccountKey=akey;"}
                           """;

        string redacted = SupportBundleRedactor.RedactSensitivePatterns(raw);

        redacted.Should().NotContain("supersecret");
        redacted.Should().NotContain("abcdef");
        redacted.Should().NotContain("hunter2");
        redacted.Should().NotContain("akey");
        redacted.Should().Contain("[REDACTED]");
    }

    [Fact]
    public async Task CollectAsync_with_mock_http_produces_all_sections()
    {
        using HttpMessageHandler handler = new StubApiHandler();
        using HttpClient http = new(handler) { BaseAddress = new Uri("http://stub.local") };
        ArchLucidApiClient client = new(http);

        ArchLucidProjectScaffolder.ArchLucidCliConfig config = new()
        {
            ProjectName = "p",
            SchemaVersion = "1.0",
            ApiUrl = "http://stub.local",
            Inputs = new ArchLucidProjectScaffolder.InputsSection { Brief = "inputs/brief.md" },
            Outputs = new ArchLucidProjectScaffolder.OutputsSection { LocalCacheDir = "outputs" },
            Plugins = new ArchLucidProjectScaffolder.PluginsSection { LockFile = "plugins/x.json" },
            Infra = new ArchLucidProjectScaffolder.InfraSection
            {
                Terraform = new ArchLucidProjectScaffolder.TerraformSection
                {
                    Enabled = false, Path = "infra/terraform"
                }
            }
        };

        string cwd = Path.Combine(Path.GetTempPath(),
            "ArchLucidSupportBundleTests." + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            Directory.CreateDirectory(cwd);
            Directory.CreateDirectory(Path.Combine(cwd, "outputs"));
            await File.WriteAllTextAsync(Path.Combine(cwd, "outputs", "x.txt"), "hello");

            SupportBundlePayload payload =
                await SupportBundleCollector.CollectAsync(client, cwd, config, CancellationToken.None);

            payload.Build.Cli.InformationalVersion.Should().NotBeNullOrWhiteSpace();
            payload.Build.ApiVersionJson.Should().Contain("informationalVersion");
            payload.Health.Ready.HttpStatus.Should().Be(200);
            payload.ApiContract.MicrosoftOpenApiV1.HttpStatus.Should().Be(200);
            payload.ApiContract.MicrosoftOpenApiV1.BodyPreview.Should().Contain("openapi");
            payload.Manifest.TriageReadOrder.Should().NotBeEmpty();
            payload.ConfigSummary.HasArchlucidJson.Should().BeTrue();
            payload.Workspace.FileCount.Should().Be(1);
        }
        finally
        {
            if (Directory.Exists(cwd))

                Directory.Delete(cwd, true);
        }
    }

    [Fact]
    public void WriteDirectory_creates_expected_files()
    {
        SupportBundlePayload payload = new(
            new SupportBundleManifest { CreatedUtc = "2026-01-01T00:00:00Z", CliWorkingDirectory = "/tmp" },
            new SupportBundleBuildSection(),
            new SupportBundleHealthSection(),
            new SupportBundleApiContractSection(),
            new SupportBundleConfigSummary(),
            new SupportBundleEnvironmentSection(),
            new SupportBundleWorkspaceSection(),
            new SupportBundleReferencesSection(),
            new SupportBundleLogsSection());

        string dir = Path.Combine(Path.GetTempPath(), "bundleWrt." + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            SupportBundleArchiveWriter.WriteDirectory(payload, dir);

            File.Exists(Path.Combine(dir, SupportBundleArchiveWriter.ManifestFileName)).Should().BeTrue();
            File.Exists(Path.Combine(dir, SupportBundleArchiveWriter.ReadmeFileName)).Should().BeTrue();
            File.Exists(Path.Combine(dir, SupportBundleArchiveWriter.HealthFileName)).Should().BeTrue();
            File.Exists(Path.Combine(dir, SupportBundleArchiveWriter.ApiContractFileName)).Should().BeTrue();
            File.ReadAllText(Path.Combine(dir, SupportBundleArchiveWriter.ManifestFileName)).Should()
                .Contain("bundleFormatVersion");
            File.ReadAllText(Path.Combine(dir, SupportBundleArchiveWriter.ReadmeFileName)).Should()
                .Contain("health.json");
        }
        finally
        {
            if (Directory.Exists(dir))

                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteZip_contains_json_entries()
    {
        SupportBundlePayload payload = new(
            new SupportBundleManifest { CreatedUtc = "2026-01-01T00:00:00Z", CliWorkingDirectory = "/x" },
            new SupportBundleBuildSection(),
            new SupportBundleHealthSection(),
            new SupportBundleApiContractSection(),
            new SupportBundleConfigSummary(),
            new SupportBundleEnvironmentSection(),
            new SupportBundleWorkspaceSection(),
            new SupportBundleReferencesSection(),
            new SupportBundleLogsSection());

        string dir = Path.Combine(Path.GetTempPath(), "bundleZip." + Guid.NewGuid().ToString("N")[..8]);
        string zip = dir + ".zip";

        try
        {
            SupportBundleArchiveWriter.WriteDirectory(payload, dir);
            SupportBundleArchiveWriter.WriteZip(dir, zip);

            File.Exists(zip).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(zip))

                File.Delete(zip);


            if (Directory.Exists(dir))

                Directory.Delete(dir, true);
        }
    }

    private sealed class StubApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;
            string json;

            if (string.Equals(path, "/version", StringComparison.Ordinal))
                json = """{"application":"ArchLucid.Api","informationalVersion":"1.0-test"}""";
            else if (string.Equals(path, "/openapi/v1.json", StringComparison.Ordinal))
                json = """{"openapi":"3.0.1","info":{"title":"ArchLucid"}}""";
            else
                json = """{"status":"Healthy"}""";


            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
