using ArchLucid.Api.Controllers.Authority;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Coordination.Compare;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="AuthorityCompareController" /> problem responses (no full host).
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuthorityCompareControllerTests
{
    [Fact]
    public async Task CompareManifests_returns_409_when_manifests_exist_in_different_scopes()
    {
        Guid leftManifestId = Guid.NewGuid();
        Guid rightManifestId = Guid.NewGuid();
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Mock<IAuthorityCompareService> compare = new();
        compare
            .Setup(c => c.CompareManifestsAsync(scope, leftManifestId, rightManifestId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot compare manifests across different scopes."));

        Mock<IScopeContextProvider> scopes = new();
        scopes.Setup(s => s.GetCurrentScope()).Returns(scope);

        AuthorityCompareController controller = new(compare.Object, scopes.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        IActionResult action =
            await controller.CompareManifests(leftManifestId, rightManifestId, CancellationToken.None);

        ObjectResult obj = action.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}
