using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;
using ArchLucid.Host.Core.Services;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Governance;

/// <summary>
///     Versioned policy pack CRUD, publish, assign, and effective-governance reads for the ambient
///     tenant/workspace/project.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Routes:</strong> under <c>v{version}/policy-packs</c>. Mutating actions require
///         <see cref="ArchLucidPolicies.AdminAuthority" />; reads require
///         <see cref="ArchLucidPolicies.ReadAuthority" />. Request bodies are validated with FluentValidation (see
///         validators for <see cref="CreatePolicyPackRequest" />, etc.).
///     </para>
///     <para>
///         <strong>Scope:</strong> All operations use <see cref="IScopeContextProvider.GetCurrentScope" /> for
///         tenant/workspace/project ids (headers or JWT claims).
///     </para>
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/policy-packs")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public sealed class PolicyPacksController(
    IScopeContextProvider scopeProvider,
    IPolicyPackRepository packRepository,
    IPolicyPackVersionRepository versionRepository,
    IPolicyPackResolver resolver,
    IEffectiveGovernanceLoader governanceLoader,
    IPolicyPacksAppService policyPacksApp)
    : ControllerBase
{
    /// <summary>Creates a new pack and an initial unpublished version <c>1.0.0</c>.</summary>
    /// <remarks>Audit: <c>PolicyPackCreated</c> via <see cref="IPolicyPacksAppService" />.</remarks>
    [HttpPost]
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [ProducesResponseType(typeof(PolicyPack), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePolicyPackRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        PolicyPack pack = await policyPacksApp.CreatePackAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request.Name,
            request.Description,
            request.PackType,
            request.InitialContentJson,
            ct);

        return Ok(pack);
    }

    /// <summary>Publishes or upserts a version for the pack and marks the pack active.</summary>
    /// <remarks>Audit: <c>PolicyPackVersionPublished</c>.</remarks>
    [HttpPost("{policyPackId:guid}/publish")]
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [ProducesResponseType(typeof(PolicyPackVersion), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Publish(
        Guid policyPackId,
        [FromBody] PublishPolicyPackVersionRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        PolicyPackVersion version = await policyPacksApp.PublishVersionAsync(
            policyPackId,
            request.Version.Trim(),
            request.ContentJson,
            ct);

        return Ok(version);
    }

    /// <summary>
    ///     Assigns an existing published version to a governance tier (<see cref="AssignPolicyPackRequest.ScopeLevel" />) for
    ///     the current scope.
    /// </summary>
    /// <returns>404 with <c>policy-pack-version-not-found</c> when the version row does not exist.</returns>
    /// <remarks>Audit: <c>PolicyPackAssignmentCreated</c>. Default scope level is Project when omitted or blank in JSON.</remarks>
    [HttpPost("{policyPackId:guid}/assign")]
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [ProducesResponseType(typeof(PolicyPackAssignment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid policyPackId,
        [FromBody] AssignPolicyPackRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        string versionKey = request.Version.Trim();
        string scopeLevel = string.IsNullOrWhiteSpace(request.ScopeLevel) ? "Project" : request.ScopeLevel;

        PolicyPackAssignment? assignment = await policyPacksApp.TryAssignAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            policyPackId,
            versionKey,
            scopeLevel,
            request.IsPinned,
            ct);

        if (assignment is null)
            return this.NotFoundProblem(
                $"Policy pack version '{versionKey}' was not found for pack '{policyPackId}'.",
                ProblemTypes.PolicyPackVersionNotFound);


        return Ok(assignment);
    }

    /// <summary>Soft-deletes a policy pack assignment for the current tenant (row retained for audit).</summary>
    /// <returns>404 when no active assignment matched.</returns>
    /// <remarks>Audit: <c>PolicyPackAssignmentArchived</c>.</remarks>
    [HttpPost("assignments/{assignmentId:guid}/archive")]
    [Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveAssignment(Guid assignmentId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        bool ok = await policyPacksApp.TryArchiveAssignmentAsync(scope.TenantId, assignmentId, ct);

        if (!ok)
            return this.NotFoundProblem(
                $"Assignment '{assignmentId}' was not found or is already archived for this tenant.",
                ProblemTypes.ResourceNotFound);


        return NoContent();
    }

    /// <summary>Lists packs whose <em>authoring</em> scope matches the current tenant/workspace/project.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyPack>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PolicyPack>>> List(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<PolicyPack> packs = await packRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(packs);
    }

    /// <summary>Lists all version rows for a pack (newest first by repository ordering).</summary>
    /// <returns>Version list, or 404 when the pack does not exist in the current scope.</returns>
    [HttpGet("{policyPackId:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyPackVersion>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListVersions(
        Guid policyPackId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        PolicyPack? pack = await packRepository.GetByIdAsync(policyPackId, ct);

        if (pack is null ||
            pack.TenantId != scope.TenantId ||
            pack.WorkspaceId != scope.WorkspaceId ||
            pack.ProjectId != scope.ProjectId)

            return this.NotFoundProblem(
                $"Policy pack '{policyPackId}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);


        IReadOnlyList<PolicyPackVersion> versions = await versionRepository.ListByPackAsync(policyPackId, ct);
        return Ok(versions);
    }

    /// <summary>
    ///     Returns each applicable enabled assignment as a separate <see cref="ResolvedPolicyPack" /> (raw <c>ContentJson</c>
    ///     per pack)—no merge.
    /// </summary>
    /// <remarks>
    ///     For merged effective document and precedence, use <c>GET …/effective-content</c> or
    ///     <c>GET …/governance-resolution</c>.
    /// </remarks>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(EffectivePolicyPackSet), StatusCodes.Status200OK)]
    public async Task<ActionResult<EffectivePolicyPackSet>> GetEffective(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        EffectivePolicyPackSet effective = await resolver.ResolveAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(effective);
    }

    /// <summary>
    ///     Returns the single merged <see cref="PolicyPackContentDocument" /> after hierarchical resolution (project &gt;
    ///     workspace &gt; tenant, pin, recency).
    /// </summary>
    /// <remarks>
    ///     Implemented via <see cref="IEffectiveGovernanceLoader" /> → <see cref="IEffectiveGovernanceResolver" />
    ///     (decisions/conflicts omitted here).
    ///     Used by alert/compliance/advisory code paths indirectly through the same loader in persistence services.
    /// </remarks>
    [HttpGet("effective-content")]
    [ProducesResponseType(typeof(PolicyPackContentDocument), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolicyPackContentDocument>> GetEffectiveContent(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        PolicyPackContentDocument doc = await governanceLoader.LoadEffectiveContentAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(doc);
    }
}
