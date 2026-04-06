namespace ArchiForge.Api.Swagger;

/// <summary>
/// Maps <c>ArchiForgeAuth:Mode</c> to an OpenAPI <c>securitySchemes</c> id for Swashbuckle.
/// </summary>
internal static class SwaggerOpenApiAuth
{
    internal const string BearerSchemeId = "Bearer";
    internal const string ApiKeySchemeId = "ApiKey";

    internal static string? ResolveSecuritySchemeId(IConfiguration configuration)
    {
        string? mode = configuration["ArchiForgeAuth:Mode"];

        if (string.IsNullOrWhiteSpace(mode))
        
            return null;
        

        if (string.Equals(mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
        
            return BearerSchemeId;
        

        return string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase) ? ApiKeySchemeId : null;
    }
}
