using System.Text.Json;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Scim;
using ArchLucid.Application.Scim.Filtering;
using ArchLucid.Core.Scim.Filtering;
using ArchLucid.Core.Scim.Models;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Scim;

[ApiController]
[ApiVersionNeutral]
[Route("scim/v2/Users")]
[Authorize(AuthenticationSchemes = ScimBearerDefaults.AuthenticationScheme, Policy = ArchLucidPolicies.ScimWrite)]
public sealed class ScimUsersController(
    IScimUserService users,
    IScopeContextProvider scopeContextProvider) : ControllerBase
{
    private readonly IScimUserService _users = users ?? throw new ArgumentNullException(nameof(users));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    [HttpGet]
    [Produces("application/scim+json")]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? filter,
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;

        try
        {
            (IReadOnlyList<ScimUserRecord> items, int total) =
                await _users.ListAsync(tenantId, filter, startIndex, count, cancellationToken);

            return ScimResourceSerializer.JsonContent(ScimResourceSerializer.ListResponse(total, startIndex, items));
        }
        catch (ScimFilterParseException ex)
        {
            return ScimErrorResultFactory.Create(400, "invalidFilter", ex.Message);
        }
        catch (ScimFilterSqlException ex)
        {
            return ScimErrorResultFactory.Create(400, "invalidFilter", ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    [Produces("application/scim+json")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;
        ScimUserRecord? u = await _users.GetAsync(tenantId, id, cancellationToken);

        if (u is null)
            return ScimErrorResultFactory.Create(404, "notFound", "User not found.");

        return ScimResourceSerializer.JsonContent(ScimResourceSerializer.User(u));
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
            ScimUserRecord created = await _users.CreateAsync(tenantId, body, cancellationToken);

            return ScimResourceSerializer.JsonContent(ScimResourceSerializer.User(created), StatusCodes.Status201Created);
        }
        catch (ScimConflictException ex)
        {
            return ScimErrorResultFactory.Create(409, "uniqueness", ex.Message);
        }
        catch (ScimSeatLimitExceededException)
        {
            return ScimErrorResultFactory.Create(403, "mutability", "Enterprise seat limit reached.");
        }
        catch (ScimUserResourceParseException ex)
        {
            return ScimErrorResultFactory.Create(400, ex.ScimType, ex.Message);
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
            await _users.ReplaceAsync(tenantId, id, body, cancellationToken);
            ScimUserRecord? u = await _users.GetAsync(tenantId, id, cancellationToken);

            if (u is null)
                return ScimErrorResultFactory.Create(404, "notFound", "User not found.");

            return ScimResourceSerializer.JsonContent(ScimResourceSerializer.User(u));
        }
        catch (ScimNotFoundException)
        {
            return ScimErrorResultFactory.Create(404, "notFound", "User not found.");
        }
        catch (ScimSeatLimitExceededException)
        {
            return ScimErrorResultFactory.Create(403, "mutability", "Enterprise seat limit reached.");
        }
        catch (ScimUserResourceParseException ex)
        {
            return ScimErrorResultFactory.Create(400, ex.ScimType, ex.Message);
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
            await _users.PatchAsync(tenantId, id, body, cancellationToken);
            ScimUserRecord? u = await _users.GetAsync(tenantId, id, cancellationToken);

            if (u is null)
                return ScimErrorResultFactory.Create(404, "notFound", "User not found.");

            return ScimResourceSerializer.JsonContent(ScimResourceSerializer.User(u));
        }
        catch (ScimNotFoundException)
        {
            return ScimErrorResultFactory.Create(404, "notFound", "User not found.");
        }
        catch (ScimSeatLimitExceededException)
        {
            return ScimErrorResultFactory.Create(403, "mutability", "Enterprise seat limit reached.");
        }
        catch (ScimUserResourceParseException ex)
        {
            return ScimErrorResultFactory.Create(400, ex.ScimType, ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [Produces("application/scim+json")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;

        try
        {
            await _users.DeactivateAsync(tenantId, id, cancellationToken);

            return NoContent();
        }
        catch (ScimNotFoundException)
        {
            return ScimErrorResultFactory.Create(404, "notFound", "User not found.");
        }
    }
}
