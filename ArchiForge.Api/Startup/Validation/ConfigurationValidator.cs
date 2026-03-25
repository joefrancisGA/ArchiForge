namespace ArchiForge.Api.Startup.Validation;

public sealed class ConfigurationValidator(
    ILogger<ConfigurationValidator> logger,
    IConfiguration configuration,
    IWebHostEnvironment environment)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<string> errors = new List<string>();

        string? connectionString = configuration.GetConnectionString("ArchiForge");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:ArchiForge is missing or empty.");
        }

        bool apiKeyEnabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);
        if (apiKeyEnabled)
        {
            string? adminKey = configuration["Authentication:ApiKey:AdminKey"];
            string? readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];
            if (string.IsNullOrWhiteSpace(adminKey) && string.IsNullOrWhiteSpace(readerKey))
            {
                errors.Add("When Authentication:ApiKey:Enabled is true, at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey must be configured.");
            }
        }

        string? agentMode = configuration["AgentExecution:Mode"];
        if (!string.IsNullOrWhiteSpace(agentMode) &&
            !string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("AgentExecution:Mode must be either 'Simulator' or 'Real'.");
        }

        if (string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            string? endpoint = configuration["AzureOpenAI:Endpoint"];
            string? apiKey = configuration["AzureOpenAI:ApiKey"];
            string? deployment = configuration["AzureOpenAI:DeploymentName"];
            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(deployment))
            {
                errors.Add("AgentExecution:Mode is 'Real' but one or more AzureOpenAI settings (Endpoint, ApiKey, DeploymentName) are missing.");
            }
        }

        if (errors.Count <= 0) return Task.CompletedTask;
        
        foreach (string error in errors)
        {
            logger.LogError("Configuration validation error: {Error}", error);
        }

        return environment.IsProduction() ? throw new InvalidOperationException("Configuration validation failed. See logs for details.") : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

