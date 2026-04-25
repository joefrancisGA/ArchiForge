using System.Text.Json;
using System.Text.Json.Nodes;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;

using ArchLucid.Application.Scim.Tokens;
using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/scim/tokens")]
[Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
public sealed class ScimTokensAdminController(
    IScimTokenIssuer tokenIssuer,
    IScimTenantTokenRepository tokens,
    IScopeContextProvider scopeContextProvider,
    IAuditService auditService) : ControllerBase
{
    private readonly IScimTokenIssuer _tokenIssuer = tokenIssuer ?? throw new ArgumentNullException(nameof(tokenIssuer));

    private readonly IScimTenantTokenRepository _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    [HttpPost]
    [Produces("application/json")]
    public async Task<IActionResult> IssueAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;
        ScimTokenIssueResult issued = await _tokenIssuer.IssueTokenAsync(tenantId, cancellationToken);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ScimTokenIssued,
                ActorUserId = User.Identity?.Name ?? "admin",
                ActorUserName = User.Identity?.Name ?? "admin",
                TenantId = tenantId,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                DataJson = JsonSerializer.Serialize(new { tokenId = issued.TokenId, publicLookupKey = issued.PublicLookupKey })
            },
            cancellationToken);

        JsonObject body = new()
        {
            ["id"] = issued.TokenId.ToString("D"),
            ["publicLookupKey"] = issued.PublicLookupKey,
            ["plaintextToken"] = issued.PlaintextToken
        };

        return new ContentResult
        {
            StatusCode = StatusCodes.Status201Created,
            Content = body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            ContentType = "application/json; charset=utf-8"
        };
    }

    [HttpGet]
    [Produces("application/json")]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;
        IReadOnlyList<ScimTokenSummaryRow> rows = await _tokens.ListForTenantAsync(tenantId, cancellationToken);

        JsonArray arr = new();

        foreach (ScimTokenSummaryRow r in rows)
        {
            arr.Add(
                new JsonObject
                {
                    ["id"] = r.Id.ToString("D"),
                    ["createdUtc"] = r.CreatedUtc.ToString("o"),
                    ["revokedUtc"] = r.RevokedUtc?.ToString("o"),
                    ["publicLookupKey"] = r.PublicLookupKey
                });
        }

        JsonObject body = new() { ["tokens"] = arr };

        return Content(
            body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            "application/json; charset=utf-8");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;
        bool ok = await _tokens.TryRevokeByIdAsync(tenantId, id, cancellationToken);

        if (!ok)
            return this.NotFoundProblem("The SCIM token was not found or could not be revoked.", type: ProblemTypes.ResourceNotFound);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ScimTokenRevoked,
                ActorUserId = User.Identity?.Name ?? "admin",
                ActorUserName = User.Identity?.Name ?? "admin",
                TenantId = tenantId,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                DataJson = JsonSerializer.Serialize(new { tokenId = id })
            },
            cancellationToken);

        return NoContent();
    }
}
