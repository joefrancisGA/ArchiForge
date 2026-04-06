using Microsoft.AspNetCore.Http;

namespace ArchiForge.Application.Common;

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
        string? name = httpContext?.User.Identity?.Name;

        return !string.IsNullOrWhiteSpace(name) ? name.Trim() : FallbackActor;
    }
}
