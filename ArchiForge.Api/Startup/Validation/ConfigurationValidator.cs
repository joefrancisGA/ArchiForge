using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Startup.Validation;

public sealed class ConfigurationValidator(
    ILogger<ConfigurationValidator> logger,
    IConfiguration configuration,
    IWebHostEnvironment environment)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        var connectionString = configuration.GetConnectionString("ArchiForge");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:ArchiForge is missing or empty.");
        }

        var apiKeyEnabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);
        if (apiKeyEnabled)
        {
            var adminKey = configuration["Authentication:ApiKey:AdminKey"];
            var readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];
            if (string.IsNullOrWhiteSpace(adminKey) && string.IsNullOrWhiteSpace(readerKey))
            {
                errors.Add("When Authentication:ApiKey:Enabled is true, at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey must be configured.");
            }
        }

        var agentMode = configuration["AgentExecution:Mode"];
        if (!string.IsNullOrWhiteSpace(agentMode) &&
            !string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("AgentExecution:Mode must be either 'Simulator' or 'Real'.");
        }

        if (string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var apiKey = configuration["AzureOpenAI:ApiKey"];
            var deployment = configuration["AzureOpenAI:DeploymentName"];
            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(deployment))
            {
                errors.Add("AgentExecution:Mode is 'Real' but one or more AzureOpenAI settings (Endpoint, ApiKey, DeploymentName) are missing.");
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                logger.LogError("Configuration validation error: {Error}", error);
            }

            if (environment.IsProduction())
            {
                throw new InvalidOperationException("Configuration validation failed. See logs for details.");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

