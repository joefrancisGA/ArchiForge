using ArchLucid.Api.Controllers.Tenancy;
using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TenantTrialControllerTests
{
    [Fact]
    public async Task GetTrialStatusAsync_returns_not_found_when_tenant_missing()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        };
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(scope.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantRecord?)null);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);
        Mock<IAuditService> audit = new();
        Mock<IBillingTrialConversionGate> gate = new();
        Mock<IOptionsMonitor<TrialLifecycleSchedulerOptions>> schedulerOpts = new();
        schedulerOpts.Setup(o => o.CurrentValue).Returns(new TrialLifecycleSchedulerOptions());

        TenantTrialController sut =
            new(tenants.Object, scopeProvider.Object, audit.Object, gate.Object, schedulerOpts.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        IActionResult result = await sut.GetTrialStatusAsync(CancellationToken.None);

        ObjectResult problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetTrialStatusAsync_returns_none_when_trial_status_blank()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            WorkspaceId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            ProjectId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")
        };
        TenantRecord tenant = new()
        {
            Id = scope.TenantId,
            Name = "t",
            Slug = "t",
            Tier = TenantTier.Free,
            CreatedUtc = DateTimeOffset.UtcNow,
            TrialRunsUsed = 1,
            TrialSeatsUsed = 0,
            TrialStatus = "   "
        };
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(scope.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);
        Mock<IAuditService> audit = new();
        Mock<IBillingTrialConversionGate> gate = new();
        Mock<IOptionsMonitor<TrialLifecycleSchedulerOptions>> schedulerOpts = new();
        schedulerOpts.Setup(o => o.CurrentValue).Returns(new TrialLifecycleSchedulerOptions());

        TenantTrialController sut =
            new(tenants.Object, scopeProvider.Object, audit.Object, gate.Object, schedulerOpts.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        IActionResult result = await sut.GetTrialStatusAsync(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        TenantTrialStatusResponse body = ok.Value.Should().BeOfType<TenantTrialStatusResponse>().Subject;
        body.Status.Should().Be("None");
        body.TrialRunsUsed.Should().Be(1);
        body.FirstCommitUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetTrialStatusAsync_echoes_first_commit_utc_on_none_branch_when_set()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            WorkspaceId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
            ProjectId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000")
        };
        DateTimeOffset committed = DateTimeOffset.Parse("2026-04-10T08:00:00+00:00");
        TenantRecord tenant = new()
        {
            Id = scope.TenantId,
            Name = "t",
            Slug = "t",
            Tier = TenantTier.Standard,
            CreatedUtc = DateTimeOffset.UtcNow,
            TrialRunsUsed = 0,
            TrialSeatsUsed = 0,
            TrialStatus = "   ",
            TrialFirstManifestCommittedUtc = committed
        };
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(scope.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);
        Mock<IAuditService> audit = new();
        Mock<IBillingTrialConversionGate> gate = new();
        Mock<IOptionsMonitor<TrialLifecycleSchedulerOptions>> schedulerOpts = new();
        schedulerOpts.Setup(o => o.CurrentValue).Returns(new TrialLifecycleSchedulerOptions());

        TenantTrialController sut =
            new(tenants.Object, scopeProvider.Object, audit.Object, gate.Object, schedulerOpts.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        IActionResult result = await sut.GetTrialStatusAsync(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        TenantTrialStatusResponse body = ok.Value.Should().BeOfType<TenantTrialStatusResponse>().Subject;
        body.Status.Should().Be("None");
        body.FirstCommitUtc.Should().Be(committed);
    }

    [Fact]
    public async Task GetTrialStatusAsync_echoes_first_commit_utc_on_active_branch()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111"),
            WorkspaceId = Guid.Parse("eeeeeeee-ffff-0000-1111-222222222222"),
            ProjectId = Guid.Parse("ffffffff-0000-1111-2222-333333333333")
        };
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(9);
        DateTimeOffset committed = DateTimeOffset.Parse("2026-03-01T00:00:00+00:00");
        TenantRecord tenant = new()
        {
            Id = scope.TenantId,
            Name = "t",
            Slug = "t",
            Tier = TenantTier.Free,
            CreatedUtc = DateTimeOffset.UtcNow,
            TrialRunsUsed = 0,
            TrialSeatsUsed = 0,
            TrialStatus = TrialLifecycleStatus.Active,
            TrialStartUtc = DateTimeOffset.UtcNow.AddDays(-1),
            TrialExpiresUtc = expires,
            TrialRunsLimit = 5,
            TrialSeatsLimit = 10,
            TrialFirstManifestCommittedUtc = committed
        };
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(scope.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);
        Mock<IAuditService> audit = new();
        Mock<IBillingTrialConversionGate> gate = new();
        Mock<IOptionsMonitor<TrialLifecycleSchedulerOptions>> schedulerOpts = new();
        schedulerOpts.Setup(o => o.CurrentValue).Returns(new TrialLifecycleSchedulerOptions());

        TenantTrialController sut =
            new(tenants.Object, scopeProvider.Object, audit.Object, gate.Object, schedulerOpts.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        IActionResult result = await sut.GetTrialStatusAsync(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        TenantTrialStatusResponse body = ok.Value.Should().BeOfType<TenantTrialStatusResponse>().Subject;
        body.Status.Should().Be(TrialLifecycleStatus.Active);
        body.FirstCommitUtc.Should().Be(committed);
    }

    [Fact]
    public async Task GetTrialStatusAsync_returns_active_payload_with_days_remaining()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(9);
        TenantRecord tenant = new()
        {
            Id = scope.TenantId,
            Name = "t",
            Slug = "t",
            Tier = TenantTier.Free,
            CreatedUtc = DateTimeOffset.UtcNow,
            TrialRunsUsed = 0,
            TrialSeatsUsed = 0,
            TrialStatus = TrialLifecycleStatus.Active,
            TrialStartUtc = DateTimeOffset.UtcNow.AddDays(-1),
            TrialExpiresUtc = expires,
            TrialRunsLimit = 5,
            TrialSeatsLimit = 10
        };
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(scope.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);
        Mock<IAuditService> audit = new();
        Mock<IBillingTrialConversionGate> gate = new();
        Mock<IOptionsMonitor<TrialLifecycleSchedulerOptions>> schedulerOpts = new();
        schedulerOpts.Setup(o => o.CurrentValue).Returns(new TrialLifecycleSchedulerOptions());

        TenantTrialController sut =
            new(tenants.Object, scopeProvider.Object, audit.Object, gate.Object, schedulerOpts.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        IActionResult result = await sut.GetTrialStatusAsync(CancellationToken.None);

        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        TenantTrialStatusResponse body = ok.Value.Should().BeOfType<TenantTrialStatusResponse>().Subject;
        body.Status.Should().Be(TrialLifecycleStatus.Active);
        body.TrialExpiresUtc.Should().Be(expires);
        body.DaysRemaining.Should().NotBeNull();
        body.DaysRemaining!.Value.Should().BeGreaterOrEqualTo(8).And.BeLessOrEqualTo(10);
    }
}
