using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Agent Result Diff Service.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AgentResultDiffServiceTests
{
    [Fact]
    public void Compare_ShouldDetectClaimAndControlChanges()
    {
        AgentResult[] left =
        [
            new()
            {
                ResultId = "R1",
                TaskId = "T1",
                RunId = "RUN-LEFT",
                AgentType = AgentType.Compliance,
                Claims = ["Managed identity required"],
                EvidenceRefs = ["policy-a"],
                Confidence = 0.90,
                ProposedChanges = new ManifestDeltaProposal
                {
                    ProposalId = "P1", SourceAgent = AgentType.Compliance, RequiredControls = ["Managed Identity"]
                }
            }
        ];

        AgentResult[] right =
        [
            new()
            {
                ResultId = "R2",
                TaskId = "T2",
                RunId = "RUN-RIGHT",
                AgentType = AgentType.Compliance,
                Claims = ["Managed identity required", "Private endpoints required"],
                EvidenceRefs = ["policy-a", "policy-b"],
                Confidence = 0.95,
                ProposedChanges = new ManifestDeltaProposal
                {
                    ProposalId = "P2",
                    SourceAgent = AgentType.Compliance,
                    RequiredControls = ["Managed Identity", "Private Endpoints"]
                }
            }
        ];

        AgentResultDiffService service = new();

        AgentResultDiffResult diff = service.Compare("RUN-LEFT", left, "RUN-RIGHT", right);

        diff.AgentDeltas.Should().ContainSingle();
        AgentResultDelta delta = diff.AgentDeltas.Single();

        delta.AgentType.Should().Be(AgentType.Compliance);
        delta.AddedClaims.Should().Contain("Private endpoints required");
        delta.AddedEvidenceRefs.Should().Contain("policy-b");
        delta.AddedRequiredControls.Should().Contain("Private Endpoints");
        delta.LeftConfidence.Should().Be(0.90);
        delta.RightConfidence.Should().Be(0.95);
    }
}
