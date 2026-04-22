using ArchLucid.Api.Controllers.Tenancy;
using ArchLucid.Api.Models.CustomerSuccess;
using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TenantCustomerSuccessControllerTests
{
    private static readonly ScopeContext Scope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Fact]
    public async Task GetHealthScoreAsync_returns_not_calculated_when_repository_returns_null()
    {
        Mock<ITenantCustomerSuccessRepository> repo = new();
        repo.Setup(r => r.GetHealthScoreAsync(Scope.TenantId, Scope.WorkspaceId, Scope.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantHealthScoreRecord?)null);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(Scope);

        TenantCustomerSuccessController sut = new(repo.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.GetHealthScoreAsync(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        TenantHealthScoreResponse body = ok.Value.Should().BeOfType<TenantHealthScoreResponse>().Subject;
        body.IsCalculated.Should().BeFalse();
    }

    [Fact]
    public async Task GetHealthScoreAsync_returns_scores_when_repository_has_row()
    {
        DateTimeOffset updated = DateTimeOffset.Parse("2026-04-19T12:00:00Z");
        TenantHealthScoreRecord row = new()
        {
            TenantId = Scope.TenantId,
            EngagementScore = 4.1M,
            BreadthScore = 3.0M,
            QualityScore = 3.0M,
            GovernanceScore = 3.5M,
            SupportScore = 3.2M,
            CompositeScore = 3.6M,
            UpdatedUtc = updated,
        };
        Mock<ITenantCustomerSuccessRepository> repo = new();
        repo.Setup(r => r.GetHealthScoreAsync(Scope.TenantId, Scope.WorkspaceId, Scope.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(Scope);

        TenantCustomerSuccessController sut = new(repo.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.GetHealthScoreAsync(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        TenantHealthScoreResponse body = ok.Value.Should().BeOfType<TenantHealthScoreResponse>().Subject;
        body.IsCalculated.Should().BeTrue();
        body.EngagementScore.Should().Be(4.1M);
        body.CompositeScore.Should().Be(3.6M);
        body.UpdatedUtc.Should().Be(updated);
    }

    [Fact]
    public async Task PostProductFeedbackAsync_returns_bad_request_when_body_null()
    {
        Mock<ITenantCustomerSuccessRepository> repo = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(Scope);

        TenantCustomerSuccessController sut = new(repo.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.PostProductFeedbackAsync(null!, CancellationToken.None);

        ObjectResult bad = result.Should().BeOfType<ObjectResult>().Subject;
        bad.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        repo.Verify(
            r => r.InsertProductFeedbackAsync(It.IsAny<ProductFeedbackSubmission>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostProductFeedbackAsync_persists_and_returns_no_content()
    {
        ProductFeedbackSubmission? captured = null;
        Mock<ITenantCustomerSuccessRepository> repo = new();
        repo.Setup(r => r.InsertProductFeedbackAsync(It.IsAny<ProductFeedbackSubmission>(), It.IsAny<CancellationToken>()))
            .Callback<ProductFeedbackSubmission, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(Scope);

        TenantCustomerSuccessController sut = new(repo.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        ProductFeedbackRequest request = new()
        {
            FindingRef = "finding-1",
            RunId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Score = 1,
            Comment = "ok",
        };

        IActionResult result = await sut.PostProductFeedbackAsync(request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(Scope.TenantId);
        captured.WorkspaceId.Should().Be(Scope.WorkspaceId);
        captured.ProjectId.Should().Be(Scope.ProjectId);
        captured.FindingRef.Should().Be("finding-1");
        captured.RunId.Should().Be(request.RunId);
        captured.Score.Should().Be(1);
        captured.Comment.Should().Be("ok");
        repo.Verify(r => r.InsertProductFeedbackAsync(It.IsAny<ProductFeedbackSubmission>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
