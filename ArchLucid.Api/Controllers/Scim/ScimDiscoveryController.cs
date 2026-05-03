using System.Text.Json;
using System.Text.Json.Nodes;

using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Scim;

[ApiController]
[ApiVersionNeutral]
[Route("scim/v2")]
[Authorize(AuthenticationSchemes = ScimBearerDefaults.AuthenticationScheme, Policy = ArchLucidPolicies.ScimWrite)]
public sealed class ScimDiscoveryController : ControllerBase
{
    [HttpGet("ServiceProviderConfig")]
    [Produces("application/scim+json")]
    public IActionResult ServiceProviderConfig()
    {
        JsonObject body = new()
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig"),
            ["patch"] = new JsonObject { ["supported"] = true },
            ["bulk"] = new JsonObject { ["supported"] = false },
            ["filter"] = new JsonObject { ["supported"] = true, ["maxResults"] = 200 },
            ["changePassword"] = new JsonObject { ["supported"] = false },
            ["sort"] = new JsonObject { ["supported"] = false },
            ["etag"] = new JsonObject { ["supported"] = false },
            ["authenticationSchemes"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "oauthbearertoken",
                    ["name"] = "OAuth Bearer Token",
                    ["description"] =
                        "Inbound SCIM calls authenticate with an ArchLucid-issued bearer secret presented as Authorization: Bearer (RFC 6750). Tokens are minted and rotated via the ArchLucid admin SCIM token API — there is no parallel OAuth authorize endpoint on the SCIM surface.",
                    ["specUri"] = "http://www.rfc-editor.org/info/rfc6750",
                    ["documentationUri"] = "https://www.rfc-editor.org/rfc/rfc7644.html"
                }
            }
        };

        return Content(
            body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            "application/scim+json; charset=utf-8");
    }

    [HttpGet("Schemas")]
    [Produces("application/scim+json")]
    public IActionResult Schemas()
    {
        JsonArray arr =
        [
            new JsonObject
            {
                ["id"] = "urn:ietf:params:scim:schemas:core:2.0:User",
                ["name"] = "User",
                ["meta"] = new JsonObject { ["resourceType"] = "Schema" }
            },

            new JsonObject
            {
                ["id"] = "urn:ietf:params:scim:schemas:core:2.0:Group",
                ["name"] = "Group",
                ["meta"] = new JsonObject { ["resourceType"] = "Schema" }
            }
        ];

        JsonObject body = new()
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:api:messages:2.0:ListResponse"),
            ["totalResults"] = arr.Count,
            ["Resources"] = arr
        };

        return Content(
            body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            "application/scim+json; charset=utf-8");
    }

    [HttpGet("ResourceTypes")]
    [Produces("application/scim+json")]
    public IActionResult ResourceTypes()
    {
        JsonArray arr =
        [
            new JsonObject
            {
                ["schemas"] = new JsonArray("urn:ietf:params:scim:schemas:core:2.0:ResourceType"),
                ["id"] = "User",
                ["name"] = "User",
                ["endpoint"] = "/Users",
                ["schema"] = "urn:ietf:params:scim:schemas:core:2.0:User"
            },

            new JsonObject
            {
                ["schemas"] = new JsonArray("urn:ietf:params:scim:schemas:core:2.0:ResourceType"),
                ["id"] = "Group",
                ["name"] = "Group",
                ["endpoint"] = "/Groups",
                ["schema"] = "urn:ietf:params:scim:schemas:core:2.0:Group"
            }
        ];

        JsonObject body = new()
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:api:messages:2.0:ListResponse"),
            ["totalResults"] = arr.Count,
            ["Resources"] = arr
        };

        return Content(
            body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            "application/scim+json; charset=utf-8");
    }
}
