using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Api.Swagger;

/// <summary>
/// Maps <c>ArchLucidAuth:Mode</c> to an OpenAPI <c>securitySchemes</c> id for Swashbuckle.
/// </summary>
internal static class SwaggerOpenApiAuth
{
    internal const string BearerSchemeId = "Bearer";
    internal const string ApiKeySchemeId = "ApiKey";

    internal static string? ResolveSecuritySchemeId(IConfiguration configuration)
    {
        string? mode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (string.IsNullOrWhiteSpace(mode))
            return null;


        if (string.Equals(mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
            return BearerSchemeId;


        return string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase) ? ApiKeySchemeId : null;
    }
}
