using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Tenancy;

[Trait("Suite", "Core")]
public sealed class TrialSeatAccountantTests
{
    [SkippableFact]
    public async Task TryReserveSeatAsync_same_user_twice_invokes_repository_twice_without_short_circuit()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.TryClaimTrialSeatAsync(tenantId, "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TrialSeatAccountant accountant = new(tenants.Object);
        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        Func<Task> twice = async () =>
        {
            await accountant.TryReserveSeatAsync(scope, "user-1", CancellationToken.None);
            await accountant.TryReserveSeatAsync(scope, "user-1", CancellationToken.None);
        };

        await twice.Should().NotThrowAsync();
        tenants.Verify(t => t.TryClaimTrialSeatAsync(tenantId, "user-1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [SkippableFact]
    public async Task TryReserveSeatAsync_empty_tenant_skips_repository()
    {
        Mock<ITenantRepository> tenants = new();
        TrialSeatAccountant accountant = new(tenants.Object);
        ScopeContext scope = new()
        {
            TenantId = Guid.Empty,
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        await accountant.TryReserveSeatAsync(scope, "user-1", CancellationToken.None);

        tenants.Verify(
            t => t.TryClaimTrialSeatAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
