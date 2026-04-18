using System.Globalization;

using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Tenancy;

[Trait("Suite", "Core")]
public sealed class TrialLimitGateTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static readonly TimeProvider FixedTime =
        new FixedUtcTimeProvider(new DateTime(2026, 4, 17, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task GuardWriteAsync_active_within_limits_does_not_throw()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.Active,
                    TrialExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    TrialRunsLimit = 10,
                    TrialRunsUsed = 3,
                    TrialSeatsLimit = 5,
                    TrialSeatsUsed = 2,
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GuardWriteAsync_active_expired_throws_Expired()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.Active,
                    TrialExpiresUtc = DateTimeOffset.Parse("2026-04-10T00:00:00Z", CultureInfo.InvariantCulture),
                    TrialRunsLimit = 10,
                    TrialRunsUsed = 0,
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        (await act.Should().ThrowAsync<TrialLimitExceededException>()).Which.Reason.Should().Be(TrialLimitReason.Expired);
    }

    [Fact]
    public async Task GuardWriteAsync_active_seats_exhausted_throws_SeatsExceeded()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.Active,
                    TrialExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
                    TrialRunsLimit = 100,
                    TrialRunsUsed = 0,
                    TrialSeatsLimit = 3,
                    TrialSeatsUsed = 3,
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        (await act.Should().ThrowAsync<TrialLimitExceededException>()).Which.Reason.Should().Be(TrialLimitReason.SeatsExceeded);
    }

    [Fact]
    public async Task GuardWriteAsync_active_runs_exhausted_throws_RunsExceeded()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.Active,
                    TrialExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
                    TrialRunsLimit = 10,
                    TrialRunsUsed = 10,
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        (await act.Should().ThrowAsync<TrialLimitExceededException>()).Which.Reason.Should().Be(TrialLimitReason.RunsExceeded);
    }

    [Fact]
    public async Task GuardWriteAsync_converted_trial_does_not_throw()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.Converted,
                    TrialRunsLimit = 10,
                    TrialRunsUsed = 99,
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GuardWriteAsync_none_trial_does_not_throw()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = null,
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GuardWriteAsync_read_only_throws_LifecycleWritesFrozen()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.ReadOnly,
                    TrialExpiresUtc = DateTimeOffset.Parse("2026-04-10T00:00:00Z", CultureInfo.InvariantCulture),
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardWriteAsync(scope, CancellationToken.None);

        (await act.Should().ThrowAsync<TrialLimitExceededException>())
            .Which.Reason.Should()
            .Be(TrialLimitReason.LifecycleWritesFrozen);
    }

    [Fact]
    public async Task GuardDeleteAsync_read_only_throws_LifecycleDeletesFrozen()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.ReadOnly,
                    TrialExpiresUtc = DateTimeOffset.Parse("2026-04-10T00:00:00Z", CultureInfo.InvariantCulture),
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardDeleteAsync(scope, CancellationToken.None);

        (await act.Should().ThrowAsync<TrialLimitExceededException>())
            .Which.Reason.Should()
            .Be(TrialLimitReason.LifecycleDeletesFrozen);
    }

    [Fact]
    public async Task GuardDeleteAsync_expired_does_not_throw()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantRecord
                {
                    Id = tenantId,
                    Name = "n",
                    Slug = "s",
                    Tier = TenantTier.Standard,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    TrialStatus = TrialLifecycleStatus.Expired,
                    TrialExpiresUtc = DateTimeOffset.Parse("2026-04-10T00:00:00Z", CultureInfo.InvariantCulture),
                });

        TrialLimitGate gate = new(tenants.Object, FixedTime);
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Func<Task> act = async () => await gate.GuardDeleteAsync(scope, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
