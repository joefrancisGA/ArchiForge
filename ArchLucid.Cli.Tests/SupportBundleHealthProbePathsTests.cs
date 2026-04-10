using System.Collections.Concurrent;
using System.Net;
using System.Text;

using ArchLucid.Cli.Support;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
/// Ensures support bundle collection probes the three health endpoints plus /version (56R).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SupportBundleHealthProbePathsTests
{
    [Fact]
    public async Task CollectAsync_requests_version_live_ready_and_combined_health_paths()
    {
        ConcurrentBag<string> paths = [];
        using PathRecordingHandler handler = new(paths);
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
                Terraform = new ArchLucidProjectScaffolder.TerraformSection { Enabled = false, Path = "infra/terraform" },
            },
        };

        string cwd = Path.Combine(Path.GetTempPath(), "ArchLucidBundlePaths." + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            Directory.CreateDirectory(cwd);
            Directory.CreateDirectory(Path.Combine(cwd, "outputs"));

            SupportBundlePayload payload = await SupportBundleCollector.CollectAsync(client, cwd, config, CancellationToken.None);

            payload.Health.Live.Path.Should().Be("/health/live");
            payload.Health.Ready.Path.Should().Be("/health/ready");
            payload.Health.Combined.Path.Should().Be("/health");

            string[] seen = paths.ToArray();
            seen.Should().Contain("/version");
            seen.Should().Contain("/health/live");
            seen.Should().Contain("/health/ready");
            seen.Should().Contain("/health");
            seen.Should().Contain("/openapi/v1.json");
        }
        finally
        {
            if (Directory.Exists(cwd))
            
                Directory.Delete(cwd, recursive: true);
            
        }
    }

    private sealed class PathRecordingHandler(ConcurrentBag<string> paths) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;
            paths.Add(path);

            string json;

            if (string.Equals(path, "/version", StringComparison.Ordinal))
                json = """{"application":"ArchLucid.Api","informationalVersion":"1.0-test"}""";
            else if (string.Equals(path, "/openapi/v1.json", StringComparison.Ordinal))
                json = """{"openapi":"3.0.1","info":{"title":"t"}}""";
            else
                json = """{"status":"Healthy"}""";
            

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
