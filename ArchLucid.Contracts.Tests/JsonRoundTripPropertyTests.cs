using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Integration;

using FluentAssertions;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Contracts.Tests;

/// <summary>Property-based JSON round-trip checks for key API contracts using <see cref="IntegrationEventJson.Options" />.</summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class JsonRoundTripPropertyTests
{
    private static readonly JsonSerializerOptions JsonOptions = IntegrationEventJson.Options;

    [Property(Arbitrary = [typeof(ContractJsonRoundTripArbitraries)], MaxTest = 100)]
    public void ArchitectureRequest_round_trips_json(ArchitectureRequest original)
    {
        string json = JsonSerializer.Serialize(original, JsonOptions);
        ArchitectureRequest? back = JsonSerializer.Deserialize<ArchitectureRequest>(json, JsonOptions);

        back.Should().NotBeNull();
        back.Should().BeEquivalentTo(original);
    }

    [Property(Arbitrary = [typeof(ContractJsonRoundTripArbitraries)], MaxTest = 100)]
    public void ArchitectureRun_round_trips_json(ArchitectureRun original)
    {
        string json = JsonSerializer.Serialize(original, JsonOptions);
        ArchitectureRun? back = JsonSerializer.Deserialize<ArchitectureRun>(json, JsonOptions);

        back.Should().NotBeNull();
        back.Should().BeEquivalentTo(original);
    }

    [Property(Arbitrary = [typeof(ContractJsonRoundTripArbitraries)], MaxTest = 100)]
    public void GovernanceApprovalRequest_round_trips_json(GovernanceApprovalRequest original)
    {
        string json = JsonSerializer.Serialize(original, JsonOptions);
        GovernanceApprovalRequest? back = JsonSerializer.Deserialize<GovernanceApprovalRequest>(json, JsonOptions);

        back.Should().NotBeNull();
        back.Should().BeEquivalentTo(original);
    }
}

/// <summary>FsCheck generators for <see cref="JsonRoundTripPropertyTests" />.</summary>
public static class ContractJsonRoundTripArbitraries
{
    private static readonly string[] GovernanceStatuses =
    [
        GovernanceApprovalStatus.Draft,
        GovernanceApprovalStatus.Submitted,
        GovernanceApprovalStatus.Approved,
        GovernanceApprovalStatus.Rejected,
        GovernanceApprovalStatus.Promoted,
        GovernanceApprovalStatus.Activated
    ];

    public static Arbitrary<ArchitectureRequest> ArchitectureRequests()
    {
        Gen<ArchitectureRequest> gen =
            from descSeed in Arb.Default.String().Generator
            from sysSeed in Arb.Default.String().Generator
            from envSeed in Arb.Default.String().Generator
            select BuildArchitectureRequest(descSeed, sysSeed, envSeed);

        return gen.ToArbitrary();
    }

    public static Arbitrary<ArchitectureRun> ArchitectureRuns()
    {
        Gen<ArchitectureRun> gen =
            from status in Gen.Elements(Enum.GetValues<ArchitectureRunStatus>())
            from taskCount in Gen.Choose(0, 6)
            from completedFlag in Gen.Choose(0, 1)
            let completed = completedFlag == 1
            from created in Arb.Default.DateTime().Generator
            let createdUtc = new DateTime(created.Ticks, DateTimeKind.Utc)
            let completedUtc = completed ? createdUtc.AddHours(1) : (DateTime?)null
            select new ArchitectureRun
            {
                RunId = Guid.NewGuid().ToString("N"),
                RequestId = Guid.NewGuid().ToString("N"),
                Status = status,
                CreatedUtc = createdUtc,
                CompletedUtc = completedUtc,
                CurrentManifestVersion = completed ? "v1" : null,
                ContextSnapshotId = null,
                GraphSnapshotId = completed ? Guid.NewGuid() : null,
                FindingsSnapshotId = null,
                GoldenManifestId = null,
                DecisionTraceId = null,
                ArtifactBundleId = null,
                TaskIds = [.. Enumerable.Range(0, taskCount).Select(static i => $"task-{i}")]
            };

        return gen.ToArbitrary();
    }

    public static Arbitrary<GovernanceApprovalRequest> GovernanceApprovalRequests()
    {
        Gen<GovernanceApprovalRequest> gen =
            from status in Gen.Elements(GovernanceStatuses)
            from hasReviewFlag in Gen.Choose(0, 1)
            let hasReview = hasReviewFlag == 1
            from requested in Arb.Default.DateTime().Generator
            let requestedUtc = new DateTime(requested.Ticks, DateTimeKind.Utc)
            from reviewer in Arb.Default.String().Generator
            from requestComment in Arb.Default.String().Generator
            select new GovernanceApprovalRequest
            {
                ApprovalRequestId = Guid.NewGuid().ToString("N"),
                RunId = Guid.NewGuid().ToString("N"),
                ManifestVersion = "v1",
                SourceEnvironment = GovernanceEnvironment.Dev,
                TargetEnvironment = GovernanceEnvironment.Test,
                Status = status,
                RequestedBy = "requester",
                ReviewedBy = hasReview ? string.IsNullOrWhiteSpace(reviewer) ? "rev" : reviewer.Trim() : null,
                RequestComment = string.IsNullOrEmpty(requestComment) ? null : requestComment,
                ReviewComment = hasReview ? "ok" : null,
                RequestedUtc = requestedUtc,
                ReviewedUtc = hasReview ? requestedUtc.AddHours(1) : null
            };

        return gen.ToArbitrary();
    }

    private static ArchitectureRequest BuildArchitectureRequest(string descSeed, string sysSeed, string envSeed)
    {
        string description = EnsureMinLength10(descSeed);
        string systemName = EnsureNonEmpty(sysSeed);
        string environment = EnsureNonEmpty(envSeed);

        return new ArchitectureRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Description = description,
            SystemName = systemName,
            Environment = environment,
            CloudProvider = CloudProvider.Azure,
            Constraints = [],
            RequiredCapabilities = [],
            Assumptions = [],
            PriorManifestVersion = null,
            InlineRequirements = [],
            Documents = [],
            PolicyReferences = [],
            TopologyHints = [],
            SecurityBaselineHints = [],
            InfrastructureDeclarations = []
        };
    }

    private static string EnsureMinLength10(string? seed)
    {
        string s = seed ?? string.Empty;

        const string pad = "0123456789";

        if (s.Length >= 10)
        {
            return s;
        }

        return s + pad[..(10 - s.Length)];
    }

    private static string EnsureNonEmpty(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            return "Sys";
        }

        return seed.Trim();
    }
}
