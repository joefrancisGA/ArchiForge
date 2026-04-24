using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Application.Analysis;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

public sealed class ConsultingDocxAnalysisRequestFactoryTests
{
    [Fact]
    public void Create_maps_include_flags_and_compare_fields()
    {
        ConsultingDocxExportRequest req = new()
        {
            IncludeEvidence = true,
            IncludeExecutionTraces = false,
            IncludeManifest = true,
            IncludeDiagram = false,
            IncludeDeterminismCheck = true,
            DeterminismIterations = 3,
            IncludeManifestCompare = true,
            CompareManifestVersion = "v9",
            IncludeAgentResultCompare = true,
            CompareRunId = "run-b"
        };

        ArchitectureAnalysisRequest mapped =
            ConsultingDocxAnalysisRequestFactory.Create("run-a", req);

        mapped.RunId.Should().Be("run-a");
        mapped.IncludeEvidence.Should().BeTrue();
        mapped.IncludeExecutionTraces.Should().BeFalse();
        mapped.IncludeManifest.Should().BeTrue();
        mapped.IncludeDiagram.Should().BeFalse();
        mapped.IncludeSummary.Should().BeTrue();
        mapped.IncludeDeterminismCheck.Should().BeTrue();
        mapped.DeterminismIterations.Should().Be(3);
        mapped.IncludeManifestCompare.Should().BeTrue();
        mapped.CompareManifestVersion.Should().Be("v9");
        mapped.IncludeAgentResultCompare.Should().BeTrue();
        mapped.CompareRunId.Should().Be("run-b");
    }
}
