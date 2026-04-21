using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace ArchLucid.Application.Common;

/// <summary>
/// HTTP-scoped actor resolution from <see cref="HttpContext.User"/> (identity name only; no RBAC).
/// </summary>
public sealed class ActorContext(IHttpContextAccessor httpContextAccessor) : IActorContext
{
    private const string FallbackActor = "api-user";

    /// <inheritdoc />
    public string GetActor()
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        ClaimsPrincipal? user = httpContext?.User;
        string? name = user?.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        // JwtBearer with MapInboundClaims=false (local PEM CI tokens) emits short claim type "name", not ClaimTypes.Name.
        string? jwtName = user?.FindFirst("name")?.Value;

        return !string.IsNullOrWhiteSpace(jwtName) ? jwtName.Trim() : FallbackActor;
    }
}
