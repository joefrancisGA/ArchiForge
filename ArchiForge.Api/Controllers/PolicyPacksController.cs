using System.Text.Json;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/policy-packs")]
[EnableRateLimiting("fixed")]
public sealed class PolicyPacksController(
    IScopeContextProvider scopeProvider,
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository,
    IPolicyPackAssignmentRepository assignmentRepository,
    IPolicyPackManagementService managementService,
    IPolicyPackResolver resolver,
    IEffectiveGovernanceLoader governanceLoader,
    IAuditService auditService)
    : ControllerBase
{
    private readonly IPolicyPackAssignmentRepository _assignmentRepository = assignmentRepository;

    [HttpPost]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(PolicyPack), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolicyPack>> Create(
        [FromBody] CreatePolicyPackRequest request,
        CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var pack = await managementService.CreatePackAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request.Name,
            request.Description ?? "",
            request.PackType,
            request.InitialContentJson ?? "{}",
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackCreated,
                DataJson = JsonSerializer.Serialize(new { pack.PolicyPackId, pack.Name, pack.PackType }),
            },
            ct);

        return Ok(pack);
    }

    [HttpPost("{policyPackId:guid}/publish")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(PolicyPackVersion), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolicyPackVersion>> Publish(
        Guid policyPackId,
        [FromBody] PublishPolicyPackVersionRequest request,
        CancellationToken ct = default)
    {
        var version = await managementService.PublishVersionAsync(
            policyPackId,
            request.Version.Trim(),
            request.ContentJson ?? "{}",
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackVersionPublished,
                DataJson = JsonSerializer.Serialize(new { policyPackId, version.Version }),
            },
            ct);

        return Ok(version);
    }

    [HttpPost("{policyPackId:guid}/assign")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(PolicyPackAssignment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid policyPackId,
        [FromBody] AssignPolicyPackRequest request,
        CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var versionKey = request.Version.Trim();
        var packVersion = await versionRepository.GetByPackAndVersionAsync(policyPackId, versionKey, ct);
        if (packVersion is null)
        {
            return this.NotFoundProblem(
                $"Policy pack version '{versionKey}' was not found for pack '{policyPackId}'.",
                ProblemTypes.PolicyPackVersionNotFound);
        }

        var assignment = await managementService.AssignAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            policyPackId,
            versionKey,
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackAssigned,
                DataJson = JsonSerializer.Serialize(
                    new { assignment.AssignmentId, policyPackId, version = assignment.PolicyPackVersion }),
            },
            ct);

        return Ok(assignment);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyPack>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PolicyPack>>> List(CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var packs = await packRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(packs);
    }

    [HttpGet("{policyPackId:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyPackVersion>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PolicyPackVersion>>> ListVersions(
        Guid policyPackId,
        CancellationToken ct = default)
    {
        var versions = await versionRepository.ListByPackAsync(policyPackId, ct);
        return Ok(versions);
    }

    [HttpGet("effective")]
    [ProducesResponseType(typeof(EffectivePolicyPackSet), StatusCodes.Status200OK)]
    public async Task<ActionResult<EffectivePolicyPackSet>> GetEffective(CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var effective = await resolver.ResolveAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(effective);
    }

    /// <summary>Merged declarative content from all enabled assignments (union + distinct IDs, advisory defaults last-wins).</summary>
    [HttpGet("effective-content")]
    [ProducesResponseType(typeof(PolicyPackContentDocument), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolicyPackContentDocument>> GetEffectiveContent(CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var doc = await governanceLoader.LoadEffectiveContentAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(doc);
    }
}
