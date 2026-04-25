using ArchLucid.Api.Controllers;
using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
public sealed class RegistrationControllerTrialRegistrationFailedTests
{
    [Fact]
    public async Task RegisterAsync_null_body_emits_TrialRegistrationFailed_and_400()
    {
        Mock<IAuditService> audit = new();
        Mock<ITenantProvisioningService> prov = new();
        Mock<ITrialTenantBootstrapService> boot = new();
        RegistrationController controller = new(prov.Object, audit.Object, boot.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Path = "/v1/register";
        controller.HttpContext.Response.Headers["X-Correlation-Id"] = "test-corr-1";

        IActionResult result = await controller.RegisterAsync(null, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.TrialRegistrationFailed
                                       && e.ActorUserId == "anonymous@request"
                                       && e.DataJson != null
                                       && e.DataJson.Contains("body_required", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        prov.Verify(
            p => p.ProvisionAsync(It.IsAny<TenantProvisioningRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_duplicate_org_emits_TrialRegistrationFailed_conflict()
    {
        Guid t = Guid.NewGuid();
        Guid w = Guid.NewGuid();
        Guid p = Guid.NewGuid();

        Mock<IAuditService> audit = new();
        Mock<ITenantProvisioningService> prov = new();
        _ = prov
            .Setup(
                p => p.ProvisionAsync(It.IsAny<TenantProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantProvisioningResult
                {
                    TenantId = t,
                    DefaultWorkspaceId = w,
                    DefaultProjectId = p,
                    WasAlreadyProvisioned = true
                });
        Mock<ITrialTenantBootstrapService> boot = new();
        RegistrationController controller = new(prov.Object, audit.Object, boot.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Path = "/v1/register";

        TenantRegistrationRequest body = new()
        {
            OrganizationName = "Dup " + Guid.NewGuid().ToString("N"), AdminEmail = "a@b.com", AdminDisplayName = "A"
        };
        IActionResult result = await controller.RegisterAsync(body, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ObjectResult or = (ObjectResult)result;
        or.StatusCode.Should().Be(409);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.TrialRegistrationFailed
                                       && e.DataJson != null
                                       && e.DataJson.Contains("duplicate_slug", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_provision_throws_invalid_operation_emits_TrialRegistrationFailed_validation()
    {
        Mock<IAuditService> audit = new();
        Mock<ITenantProvisioningService> prov = new();
        _ = prov
            .Setup(
                p => p.ProvisionAsync(It.IsAny<TenantProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated"));
        Mock<ITrialTenantBootstrapService> boot = new();
        RegistrationController controller = new(prov.Object, audit.Object, boot.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Path = "/v1/register";

        TenantRegistrationRequest body = new()
        {
            OrganizationName = "O " + Guid.NewGuid().ToString("N"), AdminEmail = "a@b.com", AdminDisplayName = "A"
        };
        IActionResult result = await controller.RegisterAsync(body, CancellationToken.None);
        ObjectResult or = (ObjectResult)result;
        or.StatusCode.Should().Be(400);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.TrialRegistrationFailed
                                       && e.DataJson != null
                                       && e.DataJson.Contains("InvalidOperationException", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
