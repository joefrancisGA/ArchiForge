using ArchiForge.Api.Authentication;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Startup;
using ArchiForge.Api.Validators;
using ArchiForge.Data.Infrastructure;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Serilog;

namespace ArchiForge.Api
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiProblemDetailsExceptionFilter>();
            });
            builder.Services.AddProblemDetails();
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddMvc().AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<ArchitectureRequestValidator>();
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddArchiForgeSwagger();

            builder.Services.AddAuthentication("ApiKey")
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });
            builder.Services.AddArchiForgeAuthorization();

            builder.Services.AddArchiForgeOpenTelemetry(builder.Configuration, builder.Environment);
            builder.Services.AddArchiForgeRateLimiting(builder.Configuration);
            builder.Services.AddArchiForgeCors(builder.Configuration);
            builder.Services.AddArchiForgeApplicationServices(builder.Configuration);

            var app = builder.Build();

            var connectionString = app.Configuration.GetConnectionString("ArchiForge");
            if (!string.IsNullOrEmpty(connectionString) && !DatabaseMigrator.Run(connectionString))
            {
                throw new InvalidOperationException("Database migration failed.");
            }

            app.UseArchiForgePipeline();
            app.Run();
        }
    }
}

public partial class Program { }
