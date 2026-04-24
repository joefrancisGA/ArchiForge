using System.Security.Claims;

using ArchLucid.Api.Controllers.Governance;
using ArchLucid.Api.Http;
using ArchLucid.Api.Models;
using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP surface for governance <c>?dryRun=true</c> (response headers and controller wiring).
/// </summary>
[Trait("Category", "Unit")]
public sealed class GovernanceControllerDryRunTests
{
    [Fact]
    public async Task SubmitApprovalRequest_WhenDryRun_SetsDryRunResponseHeader()
    {
        Mock<IGovernanceWorkflowService> workflow = new();
        workflow
            .Setup(w => w.SubmitApprovalRequestAsync(
                "r1",
                "v1",
                "dev",
                "test",
                "actor-1",
                null,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernanceApprovalRequest
                {
                    RunId = "r1",
                    ManifestVersion = "v1",
                    SourceEnvironment = "dev",
                    TargetEnvironment = "test",
                    Status = GovernanceApprovalStatus.Submitted
                });

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("actor-1");

        GovernanceController sut = new(
            workflow.Object,
            Mock.Of<IGovernanceApprovalRequestRepository>(),
            Mock.Of<IGovernancePromotionRecordRepository>(),
            Mock.Of<IGovernanceEnvironmentActivationRepository>(),
            actor.Object,
            Mock.Of<IScopeContextProvider>(),
            Mock.Of<IGovernanceDashboardService>(),
            Mock.Of<IGovernanceLineageService>(),
            Mock.Of<IGovernanceRationaleService>(),
            Mock.Of<IComplianceDriftTrendService>(),
            Mock.Of<IPolicyPackDryRunService>(),
            NullLogger<GovernanceController>.Instance);

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "tester")]))
        };
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        IActionResult actionResult = await sut.SubmitApprovalRequest(
            new CreateGovernanceApprovalRequest
            {
                RunId = "r1", ManifestVersion = "v1", SourceEnvironment = "dev", TargetEnvironment = "test"
            },
            true,
            CancellationToken.None);

        actionResult.Should().BeOfType<OkObjectResult>();
        http.Response.Headers[ArchLucidHttpHeaders.DryRun].ToString().Should().Be("true");
    }

    [Fact]
    public async Task Promote_WhenDryRun_SetsDryRunResponseHeader()
    {
        Mock<IGovernanceWorkflowService> workflow = new();
        workflow
            .Setup(w => w.PromoteAsync(
                "r1",
                "v1",
                "test",
                GovernanceEnvironment.Prod,
                "promoter",
                "apr-1",
                null,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernancePromotionRecord
                {
                    RunId = "r1",
                    ManifestVersion = "v1",
                    SourceEnvironment = "test",
                    TargetEnvironment = GovernanceEnvironment.Prod,
                    PromotedBy = "promoter",
                    ApprovalRequestId = "apr-1"
                });

        GovernanceController sut = new(
            workflow.Object,
            Mock.Of<IGovernanceApprovalRequestRepository>(),
            Mock.Of<IGovernancePromotionRecordRepository>(),
            Mock.Of<IGovernanceEnvironmentActivationRepository>(),
            Mock.Of<IActorContext>(),
            Mock.Of<IScopeContextProvider>(),
            Mock.Of<IGovernanceDashboardService>(),
            Mock.Of<IGovernanceLineageService>(),
            Mock.Of<IGovernanceRationaleService>(),
            Mock.Of<IComplianceDriftTrendService>(),
            Mock.Of<IPolicyPackDryRunService>(),
            NullLogger<GovernanceController>.Instance);

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "promoter")]))
        };
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        IActionResult actionResult = await sut.Promote(
            new CreateGovernancePromotionRequest
            {
                RunId = "r1",
                ManifestVersion = "v1",
                SourceEnvironment = "test",
                TargetEnvironment = GovernanceEnvironment.Prod,
                PromotedBy = "promoter",
                ApprovalRequestId = "apr-1"
            },
            true,
            CancellationToken.None);

        actionResult.Should().BeOfType<OkObjectResult>();
        http.Response.Headers[ArchLucidHttpHeaders.DryRun].ToString().Should().Be("true");
    }
}
