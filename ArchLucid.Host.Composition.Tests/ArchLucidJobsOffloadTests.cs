using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Host.Composition.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchLucidJobsOffloadTests
{
    [Fact]
    public void IsOffloaded_returns_true_when_slug_listed_under_indexed_keys()
    {
        Dictionary<string, string?> data = new() { ["Jobs:OffloadedToContainerJobs:0"] = "advisory-scan" };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.AdvisoryScan).Should().BeTrue();
        ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.OrphanProbe).Should().BeFalse();
    }
}
