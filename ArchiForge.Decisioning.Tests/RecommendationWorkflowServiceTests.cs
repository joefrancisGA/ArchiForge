using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Persistence.Advisory;

using FluentAssertions;

using Moq;

using Xunit;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Unit tests for <see cref="RecommendationWorkflowService"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RecommendationWorkflowServiceTests
{
    private readonly Mock<IRecommendationRepository> _repo = new();
    private readonly RecommendationWorkflowService _sut;

    public RecommendationWorkflowServiceTests()
    {
        _sut = new RecommendationWorkflowService(_repo.Object);
    }

    [Fact]
    public async Task PersistPlanAsync_NullPlan_Throws()
    {
        Func<Task> act = () => _sut.PersistPlanAsync(null!, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PersistPlanAsync_NewRecommendation_UpsertedAsProposed()
    {
        Guid recId = Guid.NewGuid();
        ImprovementPlan plan = CreatePlan(recId);

        _repo.Setup(r => r.GetByIdAsync(recId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationRecord?)null);

        RecommendationRecord? captured = null;
        _repo.Setup(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()))
            .Callback<RecommendationRecord, CancellationToken>((rec, _) => captured = rec)
            .Returns(Task.CompletedTask);

        await _sut.PersistPlanAsync(plan, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.RecommendationId.Should().Be(recId);
        captured.Status.Should().Be(RecommendationStatus.Proposed);
        captured.Title.Should().Be("Test Title");
    }

    [Fact]
    public async Task PersistPlanAsync_ExistingAccepted_PreservesStatus()
    {
        Guid recId = Guid.NewGuid();
        ImprovementPlan plan = CreatePlan(recId);

        RecommendationRecord existing = new()
        {
            RecommendationId = recId,
            Status = RecommendationStatus.Accepted,
            CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReviewedByUserId = "reviewer-1",
            ReviewedByUserName = "Reviewer One",
            ReviewComment = "Looks good",
            ResolutionRationale = "Approved"
        };

        _repo.Setup(r => r.GetByIdAsync(recId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        RecommendationRecord? captured = null;
        _repo.Setup(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()))
            .Callback<RecommendationRecord, CancellationToken>((rec, _) => captured = rec)
            .Returns(Task.CompletedTask);

        await _sut.PersistPlanAsync(plan, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(RecommendationStatus.Accepted);
        captured.ReviewedByUserId.Should().Be("reviewer-1");
        captured.CreatedUtc.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task PersistPlanAsync_ExistingStillProposed_OverwrittenAsProposed()
    {
        Guid recId = Guid.NewGuid();
        ImprovementPlan plan = CreatePlan(recId);

        RecommendationRecord existing = new()
        {
            RecommendationId = recId,
            Status = RecommendationStatus.Proposed
        };

        _repo.Setup(r => r.GetByIdAsync(recId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        RecommendationRecord? captured = null;
        _repo.Setup(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()))
            .Callback<RecommendationRecord, CancellationToken>((rec, _) => captured = rec)
            .Returns(Task.CompletedTask);

        await _sut.PersistPlanAsync(plan, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(RecommendationStatus.Proposed);
        captured.Title.Should().Be("Test Title");
    }

    [Theory]
    [InlineData(RecommendationActionType.Accept, RecommendationStatus.Accepted)]
    [InlineData(RecommendationActionType.Reject, RecommendationStatus.Rejected)]
    [InlineData(RecommendationActionType.Defer, RecommendationStatus.Deferred)]
    [InlineData(RecommendationActionType.MarkImplemented, RecommendationStatus.Implemented)]
    public async Task ApplyActionAsync_KnownAction_SetsCorrectStatus(string action, string expectedStatus)
    {
        Guid recId = Guid.NewGuid();
        RecommendationRecord record = new()
        {
            RecommendationId = recId,
            Status = RecommendationStatus.Proposed
        };

        _repo.Setup(r => r.GetByIdAsync(recId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _repo.Setup(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RecommendationActionRequest request = new() { Action = action, Comment = "note" };

        RecommendationRecord? result = await _sut.ApplyActionAsync(recId, "user-1", "User One", request, CancellationToken.None);

        Assert.NotNull(result);
        result.Status.Should().Be(expectedStatus);
        result.ReviewedByUserId.Should().Be("user-1");
        result.ReviewedByUserName.Should().Be("User One");
        result.ReviewComment.Should().Be("note");
    }

    [Fact]
    public async Task ApplyActionAsync_NonExistentRecommendation_ReturnsNull()
    {
        Guid recId = Guid.NewGuid();

        _repo.Setup(r => r.GetByIdAsync(recId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationRecord?)null);

        RecommendationActionRequest request = new() { Action = RecommendationActionType.Accept };

        RecommendationRecord? result = await _sut.ApplyActionAsync(recId, "user-1", "User One", request, CancellationToken.None);

        result.Should().BeNull();
        _repo.Verify(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyActionAsync_NullUserId_Throws()
    {
        Func<Task> act = () => _sut.ApplyActionAsync(
            Guid.NewGuid(), null!, "User", new RecommendationActionRequest { Action = "Accept" }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApplyActionAsync_NullUserName_Throws()
    {
        Func<Task> act = () => _sut.ApplyActionAsync(
            Guid.NewGuid(), "user-1", null!, new RecommendationActionRequest { Action = "Accept" }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApplyActionAsync_NullRequest_Throws()
    {
        Func<Task> act = () => _sut.ApplyActionAsync(
            Guid.NewGuid(), "user-1", "User One", null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ApplyActionAsync_UnknownAction_LeavesStatusUnchanged()
    {
        Guid recId = Guid.NewGuid();
        RecommendationRecord record = new()
        {
            RecommendationId = recId,
            Status = RecommendationStatus.Proposed
        };

        _repo.Setup(r => r.GetByIdAsync(recId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _repo.Setup(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RecommendationActionRequest request = new() { Action = "SomethingUnknown" };

        RecommendationRecord? result = await _sut.ApplyActionAsync(recId, "user-1", "User One", request, CancellationToken.None);

        Assert.NotNull(result);
        result.Status.Should().Be(RecommendationStatus.Proposed);
    }

    [Fact]
    public async Task PersistPlanAsync_MultipleRecommendations_UpsertsEach()
    {
        Guid recId1 = Guid.NewGuid();
        Guid recId2 = Guid.NewGuid();
        ImprovementPlan plan = new()
        {
            RunId = Guid.NewGuid(),
            Recommendations =
            [
                new ImprovementRecommendation
                {
                    RecommendationId = recId1, Title = "R1", Category = "Security",
                    Rationale = "r", SuggestedAction = "a", ExpectedImpact = "e", PriorityScore = 10
                },
                new ImprovementRecommendation
                {
                    RecommendationId = recId2, Title = "R2", Category = "Cost",
                    Rationale = "r", SuggestedAction = "a", ExpectedImpact = "e", PriorityScore = 5
                }
            ]
        };

        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationRecord?)null);

        _repo.Setup(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.PersistPlanAsync(plan, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _repo.Verify(r => r.UpsertAsync(It.IsAny<RecommendationRecord>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static ImprovementPlan CreatePlan(Guid recommendationId)
    {
        return new ImprovementPlan
        {
            RunId = Guid.NewGuid(),
            Recommendations =
            [
                new ImprovementRecommendation
                {
                    RecommendationId = recommendationId,
                    Title = "Test Title",
                    Category = "Security",
                    Rationale = "Test rationale",
                    SuggestedAction = "Test action",
                    ExpectedImpact = "High",
                    Urgency = "High",
                    PriorityScore = 90
                }
            ]
        };
    }
}
