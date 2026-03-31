using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchiForge.Api.Swagger;

/// <summary>
/// Registers OpenAPI <c>components.securitySchemes</c> from live <see cref="IConfiguration"/> so test hosts and
/// late configuration sources (e.g. <c>WebApplicationFactory</c>) see the correct <c>ArchiForgeAuth:Mode</c>.
/// </summary>
public sealed class OpenApiAuthSecurityDocumentFilter(IConfiguration configuration) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
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

        // Default security for all operations; anonymous actions clear this via OpenApiAuthSecurityOperationFilter.
        OpenApiSecuritySchemeReference schemeReference = new(schemeId);
        swaggerDoc.Security ??= [];
        swaggerDoc.Security.Add(new OpenApiSecurityRequirement { [schemeReference] = [] });
    }

    private static OpenApiSecurityScheme CreateBearerScheme(IConfiguration configuration)
    {
        string? audience = configuration["ArchiForgeAuth:Audience"]?.Trim();
        string? authority = configuration["ArchiForgeAuth:Authority"]?.Trim();

        string audienceNote = string.IsNullOrEmpty(audience)
            ? "Configure ArchiForgeAuth:Audience to match your Entra application ID URI (e.g. api://your-api)."
            : $"JWT **aud** must match **`{audience}`**.";

        string authorityNote = string.IsNullOrEmpty(authority)
            ? "Set ArchiForgeAuth:Authority to your tenant issuer (e.g. https://login.microsoftonline.com/{tenant-id}/v2.0)."
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
                + "Configure **Authentication:ApiKey:*** and set **ArchiForgeAuth:Mode** to **ApiKey**."
        };
    }
}
