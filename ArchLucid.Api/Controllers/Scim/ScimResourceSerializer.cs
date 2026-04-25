using System.Text.Json;
using System.Text.Json.Nodes;

using ArchLucid.Core.Scim.Models;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Scim;

internal static class ScimResourceSerializer
{
    public static JsonObject User(ScimUserRecord u)
    {
        JsonObject o = new()
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:schemas:core:2.0:User"),
            ["id"] = u.Id.ToString("D"),
            ["externalId"] = u.ExternalId,
            ["userName"] = u.UserName,
            ["active"] = u.Active,
            ["meta"] = new JsonObject
            {
                ["resourceType"] = "User",
                ["created"] = u.CreatedUtc.ToString("o"),
                ["lastModified"] = u.UpdatedUtc.ToString("o")
            }
        };

        if (!string.IsNullOrEmpty(u.DisplayName))
            o["displayName"] = u.DisplayName;

        if (!string.IsNullOrEmpty(u.ResolvedRole))
            o["title"] = u.ResolvedRole;

        return o;
    }

    public static JsonObject ListResponse(int total, int startIndex, IReadOnlyList<ScimUserRecord> items)
    {
        JsonArray arr = [];

        foreach (ScimUserRecord u in items)
            arr.Add(User(u));

        return new JsonObject
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:api:messages:2.0:ListResponse"),
            ["totalResults"] = total,
            ["startIndex"] = startIndex,
            ["itemsPerPage"] = items.Count,
            ["Resources"] = arr
        };
    }

    public static JsonObject Group(ScimGroupRecord g)
    {
        return new JsonObject
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:schemas:core:2.0:Group"),
            ["id"] = g.Id.ToString("D"),
            ["externalId"] = g.ExternalId,
            ["displayName"] = g.DisplayName,
            ["meta"] = new JsonObject
            {
                ["resourceType"] = "Group",
                ["created"] = g.CreatedUtc.ToString("o"),
                ["lastModified"] = g.UpdatedUtc.ToString("o")
            }
        };
    }

    public static JsonObject GroupListResponse(int total, int startIndex, IReadOnlyList<ScimGroupRecord> items)
    {
        JsonArray arr = [];

        foreach (ScimGroupRecord g in items)
            arr.Add(Group(g));

        return new JsonObject
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:api:messages:2.0:ListResponse"),
            ["totalResults"] = total,
            ["startIndex"] = startIndex,
            ["itemsPerPage"] = items.Count,
            ["Resources"] = arr
        };
    }

    public static ContentResult JsonContent(JsonObject body, int status = StatusCodes.Status200OK)
    {
        return new ContentResult
        {
            StatusCode = status,
            Content = body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            ContentType = "application/scim+json; charset=utf-8"
        };
    }
}
