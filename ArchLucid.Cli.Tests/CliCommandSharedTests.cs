using ArchLucid.Cli.Commands;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
public sealed class CliCommandSharedTests
{
    [Fact]
    public void ParseCloudProvider_null_or_whitespace_returns_Azure()
    {
        CliCommandShared.ParseCloudProvider(null).Should().Be(CloudProvider.Azure);
        CliCommandShared.ParseCloudProvider("   ").Should().Be(CloudProvider.Azure);
    }

    [Fact]
    public void ParseCloudProvider_non_empty_maps_to_Azure()
    {
        CliCommandShared.ParseCloudProvider("AWS").Should().Be(CloudProvider.Azure);
    }

    [Fact]
    public void BuildArchitectureRequest_maps_architecture_section()
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig config = new()
        {
            ProjectName = "Sys",
            Architecture = new ArchLucidProjectScaffolder.ArchitectureSection
            {
                Environment = "staging",
                CloudProvider = "Azure",
                Constraints = ["c1"],
                RequiredCapabilities = ["cap"],
                Assumptions = ["a1"],
                PriorManifestVersion = "v9"
            }
        };

        ArchitectureRequest request = CliCommandShared.BuildArchitectureRequest(config, "brief body");

        request.SystemName.Should().Be("Sys");
        request.Description.Should().Be("brief body");
        request.Environment.Should().Be("staging");
        request.Constraints.Should().Equal("c1");
        request.RequiredCapabilities.Should().Equal("cap");
        request.Assumptions.Should().Equal("a1");
        request.PriorManifestVersion.Should().Be("v9");
    }

    [Fact]
    public void BuildArchitectureRequest_without_architecture_uses_prod_defaults()
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig config = new() { ProjectName = "P" };

        ArchitectureRequest request = CliCommandShared.BuildArchitectureRequest(config, "x");

        request.Environment.Should().Be("prod");
        request.Constraints.Should().BeEmpty();
        request.RequiredCapabilities.Should().BeEmpty();
        request.Assumptions.Should().BeEmpty();
    }

    [Fact]
    public void WriteRunSummary_writes_expected_json_shape()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "ArchLucid.Cli.Tests.runsummary." + Guid.NewGuid().ToString("N") + ".json");

        try
        {
            List<ArchLucidApiClient.AgentTaskInfo> tasks =
            [
                new()
                {
                    TaskId = "t1",
                    RunId = "run-1",
                    Objective = "obj",
                    AgentType = AgentType.Topology,
                    Status = AgentTaskStatus.Created
                }
            ];

            DateTime created = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
            CliCommandShared.WriteRunSummary(
                path,
                "http://localhost/",
                "run-1",
                "req-1",
                ArchitectureRunStatus.TasksGenerated,
                created,
                tasks,
                "mv1");

            File.Exists(path).Should().BeTrue();
            string json = File.ReadAllText(path);
            json.Should().Contain("run-1");
            json.Should().Contain("req-1");
            json.Should().Contain("t1");
            json.Should().Contain("manifest/mv1");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryLoadConfigFromCwd_without_manifest_returns_null()
    {
        string previous = Directory.GetCurrentDirectory();
        using TempDirectory temp = new();

        try
        {
            Directory.SetCurrentDirectory(temp.Path);

            ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();

            config.Should().BeNull();
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public string Path
        {
            get;
        } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "ArchLucid.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
