using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

using ArchLucidFindingEngine;

namespace ArchLucidFindingEngine.Tests;

public sealed class PluginSampleFindingEngineTests
{
    [Fact]
    public async Task AnalyzeAsync_returns_one_informational_finding()
    {
        PluginSampleFindingEngine sut = new();
        GraphSnapshot graph = new() { GraphSnapshotId = Guid.NewGuid() };

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(graph, CancellationToken.None);

        Assert.Single(findings);
        Assert.Equal("plugin-sample", sut.EngineType);
        Assert.Equal(FindingSeverity.Information, findings[0].Severity);
    }
}
