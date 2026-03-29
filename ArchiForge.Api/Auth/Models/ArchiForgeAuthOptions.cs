namespace ArchiForge.Api.Auth.Models;

public class ArchiForgeAuthOptions
{
    public const string SectionName = "ArchiForgeAuth";

    /// <summary>DevelopmentBypass | JwtBearer | ApiKey</summary>
    public string Mode { get; set; } = "DevelopmentBypass";

    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string DevUserId { get; set; } = "dev-user";
    public string DevUserName { get; set; } = "Developer";

    /// <summary>Admin | Operator | Reader</summary>
    public string DevRole { get; set; } = "Admin";
}
