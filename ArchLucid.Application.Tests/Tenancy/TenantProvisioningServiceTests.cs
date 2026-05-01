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
    [SkippableFact]
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

    [SkippableFact]
    public async Task ProvisionAsync_uses_audit_actor_override_when_set()
    {
        Mock<ITenantRepository> repo = new();
        repo.Setup(r => r.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantRecord?)null);
        repo.Setup(r => r.InsertTenantAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TenantTier>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int?>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.InsertWorkspaceAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("admin@test");
        Mock<IAuditService> audit = new();

        TenantProvisioningService sut = new(
            repo.Object,
            actor.Object,
            audit.Object,
            NullLogger<TenantProvisioningService>.Instance);

        TenantProvisioningRequest req = new()
        {
            Name = "Override Co",
            AdminEmail = "owner@override.example",
            Tier = TenantTier.Free,
            AuditActorOverride = "self-service@override.example",
        };

        await sut.ProvisionAsync(req, CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.ActorUserId == "self-service@override.example"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task ProvisionAsync_passes_entra_tenant_id_to_repository()
    {
        Guid entraTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Mock<ITenantRepository> repo = new();
        repo.Setup(r => r.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantRecord?)null);
        repo.Setup(r => r.InsertTenantAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TenantTier>(),
                It.Is<Guid?>(g => g == entraTenantId),
                It.IsAny<CancellationToken>(),
                It.IsAny<int?>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        repo.Setup(r => r.InsertWorkspaceAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("admin@test");
        Mock<IAuditService> audit = new();

        TenantProvisioningService sut = new(
            repo.Object,
            actor.Object,
            audit.Object,
            NullLogger<TenantProvisioningService>.Instance);

        TenantProvisioningRequest req = new()
        {
            Name = "Entra Linked Org",
            AdminEmail = "admin@entra.example",
            Tier = TenantTier.Enterprise,
            EntraTenantId = entraTenantId,
        };

        await sut.ProvisionAsync(req, CancellationToken.None);

        repo.Verify();
    }
}
