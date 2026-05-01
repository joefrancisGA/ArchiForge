namespace ArchLucid.Cli.Real;

/// <summary>Validates host environment variables required before applying the real Azure OpenAI compose overlay.</summary>
internal static class RealModePreflight
{
    private const string EndpointKey = "AZURE_OPENAI_ENDPOINT";
    private const string ApiKeyKey = "AZURE_OPENAI_API_KEY";
    private const string DeploymentKey = "AZURE_OPENAI_DEPLOYMENT_NAME";

    public static RealModePreflightResult Validate()
    {
        List<string> missing = [];

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EndpointKey)))
            missing.Add(EndpointKey);

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApiKeyKey)))
            missing.Add(ApiKeyKey);

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DeploymentKey)))
            missing.Add(DeploymentKey);

        if (missing.Count == 0)
            return new RealModePreflightResult(true, missing, null);

        string joined = string.Join(", ", missing);

        return new RealModePreflightResult(
            false,
            missing,
            $"Missing required environment variable(s) for real Azure OpenAI: {joined}. Set them in your shell before running `archlucid try --real`.");
    }
}
