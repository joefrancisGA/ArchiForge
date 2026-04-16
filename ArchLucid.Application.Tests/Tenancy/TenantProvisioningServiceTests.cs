using ArchLucid.Application.Common;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Tenancy;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TenantProvisioningServiceTests
{
    [Fact]
    public async Task ProvisionAsync_is_idempotent_by_slug()
    {
        InMemoryTenantRepository repo = new();
        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("admin@test");
        Mock<IAuditService> audit = new();

        TenantProvisioningService sut = new(repo, actor.Object, audit.Object, NullLogger<TenantProvisioningService>.Instance);

        TenantProvisioningRequest req = new()
        {
            Name = "Contoso Labs",
            AdminEmail = "ops@contoso.example",
            Tier = TenantTier.Enterprise,
        };

        TenantProvisioningResult first = await sut.ProvisionAsync(req, CancellationToken.None);
        TenantProvisioningResult second = await sut.ProvisionAsync(req, CancellationToken.None);

        second.WasAlreadyProvisioned.Should().BeTrue();
        second.TenantId.Should().Be(first.TenantId);
        second.DefaultWorkspaceId.Should().Be(first.DefaultWorkspaceId);
        second.DefaultProjectId.Should().Be(first.DefaultProjectId);

        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
