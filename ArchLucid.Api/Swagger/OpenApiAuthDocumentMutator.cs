using Microsoft.OpenApi;

namespace ArchLucid.Api.Swagger;

/// <summary>
/// Shared mutation for <see cref="OpenApiDocument"/> auth metadata (Swashbuckle + Microsoft OpenAPI).
/// </summary>
internal static class OpenApiAuthDocumentMutator
{
    internal static void Apply(OpenApiDocument swaggerDoc, IConfiguration configuration)
    {
        string? schemeId = SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration);

        if (string.IsNullOrEmpty(schemeId))
        {
            return;
        }

        swaggerDoc.Components ??= new OpenApiComponents();

        if (string.Equals(schemeId, SwaggerOpenApiAuth.BearerSchemeId, StringComparison.Ordinal))
        {
            swaggerDoc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            swaggerDoc.Components.SecuritySchemes[SwaggerOpenApiAuth.BearerSchemeId] = CreateBearerScheme(configuration);
        }
        else if (string.Equals(schemeId, SwaggerOpenApiAuth.ApiKeySchemeId, StringComparison.Ordinal))
        {
            swaggerDoc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            swaggerDoc.Components.SecuritySchemes[SwaggerOpenApiAuth.ApiKeySchemeId] = CreateApiKeyScheme();
        }

        OpenApiSecuritySchemeReference schemeReference = new(schemeId);
        swaggerDoc.Security ??= [];
        swaggerDoc.Security.Add(new OpenApiSecurityRequirement { [schemeReference] = [] });
    }

    private static OpenApiSecurityScheme CreateBearerScheme(IConfiguration configuration)
    {
        string? audience = configuration["ArchiForgeAuth:Audience"]?.Trim();
        string? authority = configuration["ArchiForgeAuth:Authority"]?.Trim();

        string audienceNote = string.IsNullOrEmpty(audience)
            ? "Configure ArchLucidAuth:Audience (or ArchiForgeAuth:Audience) to match your Entra application ID URI (e.g. api://your-api)."
            : $"JWT **aud** must match **`{audience}`**.";

        string authorityNote = string.IsNullOrEmpty(authority)
            ? "Set ArchLucidAuth:Authority (or ArchiForgeAuth:Authority) to your tenant issuer (e.g. https://login.microsoftonline.com/{tenant-id}/v2.0)."
            : $"Tokens must be issued by **`{authority}`**.";

        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Microsoft Entra ID (Azure AD) access token. "
                + audienceNote
                + " "
                + authorityNote
                + " App roles (**Admin**, **Operator**, **Reader**) are carried in the **`roles`** claim."
        };
    }

    private static OpenApiSecurityScheme CreateApiKeyScheme()
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Description =
                "Static API key. Send **`X-Api-Key`** on each request. "
                + "Configure **Authentication:ApiKey:*** and set **ArchLucidAuth:Mode** (or **ArchiForgeAuth:Mode**) to **ApiKey**."
        };
    }
}
