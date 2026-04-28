using ArchLucid.Application.Architecture;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Tests.Architecture;

[Trait("Suite", "Application")]
[Trait("Category", "Unit")]
public sealed class RunRoiEstimatorTests
{
    [Fact]
    public void Estimate_sums_inputs_with_configured_multipliers()
    {
        RunRoiEstimatorOptions opts = new()
        {
            HoursPerArchitectureFinding = 2,
            HoursPerManifestModeledElement = 1,
            HoursPerDecisionTrace = 1,
            HoursPerCompletedAgentResult = 1
        };

        IRunRoiEstimator sut = new RunRoiEstimator(Options.Create(opts));

        var detail = new ArchitectureRunDetail
        {
            Run = new ArchitectureRun
            {
                RunId = Guid.NewGuid().ToString("N"),
                RequestId = "req"
            },
            Results =
            [
                new AgentResult
                {
                    ResultId = "a",
                    TaskId = "t1",
                    RunId = "r",
                    AgentType = AgentType.Topology,
                    Findings =
                    [
                        new ArchitectureFinding { FindingId = "f1" },
                        new ArchitectureFinding { FindingId = "f2" }
                    ]
                },
                new AgentResult
                {
                    ResultId = "b",
                    TaskId = "t2",
                    RunId = "r",
                    AgentType = AgentType.Cost
                }
            ],
            Manifest = null,
            DecisionTraces =
            [
                RunEventTrace.From(new RunEventTracePayload { RunId = "r", EventType = "a" }),
                RunEventTrace.From(new RunEventTracePayload { RunId = "r", EventType = "b" })
            ]
        };

        RunRoiScorecardDto r = sut.Estimate(detail);

        Assert.Equal(2, r.AgentFindingTotalCount);
        Assert.Equal(2, r.CompletedAgentResultCount);
        Assert.Equal(0, r.ManifestModeledElementApproxCount);
        Assert.Equal(2, r.DecisionTraceCount);
        Assert.Equal(8, r.EstimatedManualHoursSaved);
        Assert.Equal(detail.Run.RunId, r.RunId);
    }
}
