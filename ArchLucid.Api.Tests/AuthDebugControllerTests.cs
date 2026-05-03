using System.Security.Claims;

using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Api.Models;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AuthDebugControllerTests
{
    [SkippableFact]
    public async Task Me_returns_ok_with_name_and_claims_and_committed_review_flag_async()
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.Name, "alice"), new Claim("tid", "tenant-1")],
            "test");
        DefaultHttpContext httpContext = new() { User = new ClaimsPrincipal(identity) };

        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        Mock<ICommittedArchitectureReviewFlagReader> flagReader = new();
        flagReader
            .Setup(f => f.TenantHasCommittedArchitectureReviewAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        AuthDebugController sut = new(scopeProvider.Object, flagReader.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        IActionResult result = await sut.Me(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        CallerIdentityResponse body = ok.Value.Should().BeOfType<CallerIdentityResponse>().Subject;
        body.Name.Should().Be("alice");
        body.Claims.Should().HaveCount(2);
        body.Claims.Should().ContainSingle(c => c.Type == ClaimTypes.Name && c.Value == "alice");
        body.Claims.Should().ContainSingle(c => c.Type == "tid" && c.Value == "tenant-1");
        body.HasCommittedArchitectureReview.Should().BeTrue();
    }

    [SkippableFact]
    public async Task Me_returns_ok_when_identity_name_is_null_and_flag_false()
    {
        ClaimsIdentity identity = new([], "test");
        DefaultHttpContext httpContext = new() { User = new ClaimsPrincipal(identity) };

        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        Mock<ICommittedArchitectureReviewFlagReader> flagReader = new();
        flagReader
            .Setup(f => f.TenantHasCommittedArchitectureReviewAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        AuthDebugController sut = new(scopeProvider.Object, flagReader.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        IActionResult result = await sut.Me(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        CallerIdentityResponse body = ok.Value.Should().BeOfType<CallerIdentityResponse>().Subject;
        body.Name.Should().BeNull();
        body.Claims.Should().BeEmpty();
        body.HasCommittedArchitectureReview.Should().BeFalse();
    }
}
