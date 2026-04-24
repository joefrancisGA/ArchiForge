using ArchLucid.Api.Controllers.Planning;
using ArchLucid.Core.Scoping;
using ArchLucid.Retrieval.Models;
using ArchLucid.Retrieval.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RetrievalControllerTests
{
    [Fact]
    public async Task Search_returns_bad_request_when_query_missing()
    {
        Mock<IRetrievalQueryService> retrieval = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        RetrievalController sut = new(retrieval.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.Search("   ", null, null, 8, CancellationToken.None);

        ObjectResult problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        retrieval.Verify(r => r.SearchAsync(It.IsAny<RetrievalQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_clamps_top_k_below_one_to_eight()
    {
        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid workspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid projectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Mock<IRetrievalQueryService> retrieval = new();
        retrieval
            .Setup(r => r.SearchAsync(It.IsAny<RetrievalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId });
        RetrievalController sut = new(retrieval.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.Search("hello", null, null, 0, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        retrieval.Verify(
            r => r.SearchAsync(
                It.Is<RetrievalQuery>(q => q.TopK == 8 && q.QueryText == "hello" && q.TenantId == tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_caps_top_k_at_fifty()
    {
        Guid tenantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        Guid workspaceId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        Guid projectId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        Mock<IRetrievalQueryService> retrieval = new();
        retrieval
            .Setup(r => r.SearchAsync(It.IsAny<RetrievalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId });
        RetrievalController sut = new(retrieval.Object, scopeProvider.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult result = await sut.Search("scope", null, null, 200, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        retrieval.Verify(
            r => r.SearchAsync(It.Is<RetrievalQuery>(q => q.TopK == 50), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
