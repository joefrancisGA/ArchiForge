using System.Text.Json;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Scim;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scim.Models;
using ArchLucid.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Scim;

[ApiController]
[ApiVersionNeutral]
[Route("scim/v2/Groups")]
[Authorize(AuthenticationSchemes = ScimBearerDefaults.AuthenticationScheme, Policy = ArchLucidPolicies.ScimWrite)]
public sealed class ScimGroupsController(
    IScimGroupService groups,
    IScopeContextProvider scopeContextProvider) : ControllerBase
{
    private readonly IScimGroupService _groups = groups ?? throw new ArgumentNullException(nameof(groups));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    [HttpGet]
    [Produces("application/scim+json")]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;
        (IReadOnlyList<ScimGroupRecord> items, int total) =
            await _groups.ListAsync(tenantId, startIndex, count, cancellationToken);

        return ScimResourceSerializer.JsonContent(ScimResourceSerializer.GroupListResponse(total, startIndex, items));
    }

    [HttpGet("{id:guid}")]
    [Produces("application/scim+json")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;
        ScimGroupRecord? g = await _groups.GetAsync(tenantId, id, cancellationToken);

        return g is null ? ScimErrorResultFactory.Create(404, "notFound", "Group not found.") : ScimResourceSerializer.JsonContent(ScimResourceSerializer.Group(g));
    }

    [HttpPost]
    [Produces("application/scim+json")]
    public async Task<IActionResult> CreateAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;

        JsonElement body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: cancellationToken);
        }
        catch
        {
            return ScimErrorResultFactory.Create(400, "invalidSyntax", "Invalid JSON body.");
        }

        try
        {
            ScimGroupRecord g = await _groups.CreateAsync(tenantId, body, cancellationToken);

            return ScimResourceSerializer.JsonContent(ScimResourceSerializer.Group(g), StatusCodes.Status201Created);
        }
        catch (ScimUserResourceParseException ex)
        {
            return ScimErrorResultFactory.FromParseException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Produces("application/scim+json")]
    public async Task<IActionResult> ReplaceAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;

        JsonElement body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: cancellationToken);
        }
        catch
        {
            return ScimErrorResultFactory.Create(400, "invalidSyntax", "Invalid JSON body.");
        }

        try
        {
            await _groups.ReplaceAsync(tenantId, id, body, cancellationToken);
            ScimGroupRecord? g = await _groups.GetAsync(tenantId, id, cancellationToken);

            return g is null ? ScimErrorResultFactory.Create(404, "notFound", "Group not found.") : ScimResourceSerializer.JsonContent(ScimResourceSerializer.Group(g));
        }
        catch (ScimNotFoundException)
        {
            return ScimErrorResultFactory.Create(404, "notFound", "Group not found.");
        }
        catch (ScimUserResourceParseException ex)
        {
            return ScimErrorResultFactory.FromParseException(ex);
        }
    }

    [HttpPatch("{id:guid}")]
    [Produces("application/scim+json")]
    public async Task<IActionResult> PatchAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;

        JsonElement body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: cancellationToken);
        }
        catch
        {
            return ScimErrorResultFactory.Create(400, "invalidSyntax", "Invalid JSON body.");
        }

        try
        {
            await _groups.PatchMembersAsync(tenantId, id, body, cancellationToken);
            ScimGroupRecord? g = await _groups.GetAsync(tenantId, id, cancellationToken);

            return g is null ? ScimErrorResultFactory.Create(404, "notFound", "Group not found.") : ScimResourceSerializer.JsonContent(ScimResourceSerializer.Group(g));
        }
        catch (ScimNotFoundException)
        {
            return ScimErrorResultFactory.Create(404, "notFound", "Group not found.");
        }
        catch (ScimUserResourceParseException ex)
        {
            return ScimErrorResultFactory.FromParseException(ex);
        }
    }
}
