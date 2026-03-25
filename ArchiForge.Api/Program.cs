using ArchiForge.Api.Auth.Services;
using ArchiForge.Api.Configuration;
using ArchiForge.Api.Startup;
using ArchiForge.Application.Governance.Preview;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Sql;

using Serilog;

namespace ArchiForge.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        // Add services to the container.

        builder.Services.AddArchiForgeMvc();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchiForgeAuth(builder.Configuration);
        builder.Services.AddArchiForgeAuthorization();

        builder.Services.AddArchiForgeOpenTelemetry(builder.Configuration, builder.Environment);
        builder.Services.AddArchiForgeRateLimiting(builder.Configuration);
        builder.Services.AddArchiForgeCors(builder.Configuration);
        builder.Services.AddArchiForgeApplicationServices(builder.Configuration);
        builder.Services.AddScoped<IGovernancePreviewService, GovernancePreviewService>();

        WebApplication app = builder.Build();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            ArchiForgeOptions? options = config.GetSection(ArchiForgeOptions.SectionName).Get<ArchiForgeOptions>();

            if (string.Equals(options?.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase))
            {
                ISchemaBootstrapper bootstrapper = scope.ServiceProvider.GetRequiredService<ISchemaBootstrapper>();
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                bootstrapper.EnsureSchemaAsync(cts.Token).GetAwaiter().GetResult();
            }
        }

        string? connectionString = app.Configuration.GetConnectionString("ArchiForge");
        if (!string.IsNullOrEmpty(connectionString) && !DatabaseMigrator.Run(connectionString))
        {
            throw new InvalidOperationException("Database migration failed.");
        }

        app.UseArchiForgePipeline();
        app.Run();
    }
}

public partial class Program;
