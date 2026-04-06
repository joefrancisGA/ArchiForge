using System.Security.Claims;

namespace ArchiForge.Api.Auth.Models;

public class ArchiForgeAuthOptions
{
    public const string SectionName = "ArchiForgeAuth";

    /// <summary>DevelopmentBypass | JwtBearer | ApiKey</summary>
    public string Mode { get; set; } = "DevelopmentBypass";
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Claim type used as the user name after JWT validation. Entra ID tokens often use
    /// <c>preferred_username</c> or <c>name</c>; default matches classic <see cref="ClaimTypes.Name"/>.
    /// </summary>
    public string NameClaimType { get; set; } = ClaimTypes.Name;
    public string DevUserId { get; set; } = "dev-user";
    public string DevUserName { get; set; } = "Developer";

    /// <summary>Admin | Operator | Reader</summary>
    public string DevRole { get; set; } = "Admin";
}
