using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.ProblemDetails;

public static class ScimErrorResultFactory
{
    public static IActionResult Create(int status, string scimType, string detail)
    {
        JsonObject body = new()
        {
            ["schemas"] = new JsonArray("urn:ietf:params:scim:api:messages:2.0:Error"),
            ["status"] = status.ToString(CultureInfo.InvariantCulture),
            ["scimType"] = scimType,
            ["detail"] = detail
        };

        ContentResult result = new()
        {
            StatusCode = status,
            Content = body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            ContentType = "application/scim+json; charset=utf-8"
        };

        return result;
    }
}
