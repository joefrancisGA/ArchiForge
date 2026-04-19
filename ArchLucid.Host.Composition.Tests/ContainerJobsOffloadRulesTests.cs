using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Startup.Validation.Rules;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Host.Composition.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ContainerJobsOffloadRulesTests
{
    [Fact]
    public void Collect_Production_Worker_offloaded_without_deployed_manifest_adds_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Jobs:OffloadedToContainerJobs:0"] = "advisory-scan",
            ["Jobs:DeployedContainerJobNames"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        IHostEnvironment environment = new CompositionTestHostEnvironment(Environments.Production);
        List<string> errors = [];

        ContainerJobsOffloadRules.Collect(
            configuration,
            environment,
            ArchLucidHostingRole.Worker,
            errors);

        errors.Should().ContainSingle(static e => e.Contains("advisory-scan", StringComparison.Ordinal)
                                                 && e.Contains("DeployedContainerJobNames", StringComparison.Ordinal));
    }

    [Fact]
    public void Collect_Production_Worker_offloaded_with_deployed_manifest_is_clean()
    {
        Dictionary<string, string?> data = new()
        {
            ["Jobs:OffloadedToContainerJobs:0"] = "advisory-scan",
            ["Jobs:DeployedContainerJobNames"] = "advisory-scan,data-archival",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        IHostEnvironment environment = new CompositionTestHostEnvironment(Environments.Production);
        List<string> errors = [];

        ContainerJobsOffloadRules.Collect(
            configuration,
            environment,
            ArchLucidHostingRole.Worker,
            errors);

        errors.Should().BeEmpty();
    }
}
