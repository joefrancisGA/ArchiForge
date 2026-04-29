using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace ArchLucid.Application.Common;

/// <summary>
/// HTTP-scoped actor resolution from <see cref="HttpContext.User"/> (display name + JWT object id for SoD).
/// </summary>
public sealed class ActorContext(IHttpContextAccessor httpContextAccessor) : IActorContext
{
    internal const string JwtActorKeyPrefix = "jwt:";

    private const string FallbackActor = "api-user";

    private const string TidClaimType = "tid";
    private const string OidShortClaimType = "oid";
    private const string OidLongClaimType =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

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

    /// <inheritdoc />
    public string GetActorId()
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        ClaimsPrincipal? user = httpContext?.User;
        string? oid = TryGetClaimValue(user, OidShortClaimType) ?? TryGetClaimValue(user, OidLongClaimType);

        if (string.IsNullOrWhiteSpace(oid))
            return GetActor();

        string oidNormalized = oid.Trim();
        string? tid = TryGetClaimValue(user, TidClaimType);

        if (string.IsNullOrWhiteSpace(tid))
            return $"{JwtActorKeyPrefix}{oidNormalized}";

        return $"{JwtActorKeyPrefix}{tid.Trim()}:{oidNormalized}";
    }

    private static string? TryGetClaimValue(ClaimsPrincipal? user, string claimType)
    {
        Claim? first = user?.FindFirst(claimType);

        if (string.IsNullOrWhiteSpace(first?.Value))
            return null;


        return first.Value;
    }
}
