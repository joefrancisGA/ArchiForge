using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Api.OpenApi;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Validators;

using Asp.Versioning;

using FluentValidation;
using FluentValidation.AspNetCore;

namespace ArchiForge.Api.Startup;

internal static class MvcExtensions
{
    public static IServiceCollection AddArchiForgeMvc(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.Filters.Add<ApiProblemDetailsExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                // Contract enums as strings (e.g. run.status, agentType) so clients and integration tests match OpenAPI expectations.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true));
            });
        services.AddProblemDetails();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<ArchitectureRequestValidator>();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<MicrosoftOpenApiAuthDocumentTransformer>();
            options.AddDocumentTransformer<MicrosoftOpenApiCodeGenFriendlySchemaTransformer>();
            options.AddOperationTransformer<MicrosoftOpenApiAnonymousSecurityOperationTransformer>();
        });
        services.AddEndpointsApiExplorer();
        services.AddArchiForgeSwagger();
        return services;
    }
}
