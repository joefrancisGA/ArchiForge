using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

using FluentAssertions;

namespace ArchiForge.Contracts.Tests;

/// <summary>
/// Ensures core API/DTO shapes round-trip through System.Text.Json the same way HTTP payloads do.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class KeyContractsJsonRoundTripTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public void ArchitectureRequest_round_trips_json()
    {
        ArchitectureRequest original = new()
        {
            RequestId = "req-roundtrip-1",
            Description = "At least ten chars for validation-friendly samples in contracts tests.",
            SystemName = "BillingSvc",
            Environment = "staging",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["c1", "c2"],
        };

        string json = JsonSerializer.Serialize(original, JsonOptions);
        ArchitectureRequest? back = JsonSerializer.Deserialize<ArchitectureRequest>(json, JsonOptions);

        back.Should().NotBeNull();
        back!.RequestId.Should().Be(original.RequestId);
        back.Description.Should().Be(original.Description);
        back.SystemName.Should().Be(original.SystemName);
        back.Environment.Should().Be(original.Environment);
        back.CloudProvider.Should().Be(original.CloudProvider);
        back.Constraints.Should().Equal(original.Constraints);
    }

    [Fact]
    public void AgentTask_round_trips_json()
    {
        AgentTask original = new()
        {
            TaskId = "task-rt-1",
            RunId = "run-rt-1",
            AgentType = AgentType.Topology,
            Objective = "Evaluate topology proposal.",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc),
        };

        string json = JsonSerializer.Serialize(original, JsonOptions);
        AgentTask? back = JsonSerializer.Deserialize<AgentTask>(json, JsonOptions);

        back.Should().NotBeNull();
        back!.TaskId.Should().Be(original.TaskId);
        back.RunId.Should().Be(original.RunId);
        back.AgentType.Should().Be(original.AgentType);
        back.Objective.Should().Be(original.Objective);
        back.Status.Should().Be(original.Status);
        back.CreatedUtc.Should().Be(original.CreatedUtc);
    }

    [Fact]
    public void DecisionNode_with_options_round_trips_json()
    {
        DecisionOption optA = new()
        {
            Description = "Accept",
            BaseConfidence = 0.7,
            SupportScore = 0.1,
            OppositionScore = 0.05,
            EvidenceRefs = ["e1"],
        };

        DecisionOption optB = new()
        {
            Description = "Reject",
            BaseConfidence = 0.2,
            SupportScore = 0,
            OppositionScore = 0,
        };

        DecisionNode original = new()
        {
            RunId = "run-dn-1",
            Topic = "TopologyAcceptance",
            Options = [optA, optB],
            SelectedOptionId = optA.OptionId,
            Confidence = optA.FinalScore,
            Rationale = "Kept topology.",
            SupportingEvaluationIds = ["ev1"],
            OpposingEvaluationIds = [],
            CreatedUtc = new DateTime(2026, 4, 5, 13, 0, 0, DateTimeKind.Utc),
        };

        string json = JsonSerializer.Serialize(original, JsonOptions);
        DecisionNode? back = JsonSerializer.Deserialize<DecisionNode>(json, JsonOptions);

        back.Should().NotBeNull();
        back!.RunId.Should().Be(original.RunId);
        back.Topic.Should().Be(original.Topic);
        back.Options.Should().HaveCount(2);
        back.SelectedOptionId.Should().Be(original.SelectedOptionId);
        back.Confidence.Should().BeApproximately(original.Confidence, 1e-9);
        back.Options[0].FinalScore.Should().BeApproximately(optA.FinalScore, 1e-9);
        back.SupportingEvaluationIds.Should().Equal(original.SupportingEvaluationIds);
    }
}
