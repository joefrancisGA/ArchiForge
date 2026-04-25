namespace ArchLucid.Decisioning.Validation;

/// <summary>
///     <see cref="ISchemaValidationService" /> that always succeeds. Use in tests that exercise merge logic without JSON
///     schema files.
/// </summary>
public sealed class PassthroughSchemaValidationService : ISchemaValidationService
{
    public SchemaValidationResult ValidateAgentResultJson(string json)
    {
        return new SchemaValidationResult();
    }

    public SchemaValidationResult ValidateGoldenManifestJson(string json)
    {
        return new SchemaValidationResult();
    }

    public SchemaValidationResult ValidateExplanationRunJson(string json)
    {
        return new SchemaValidationResult();
    }

    public SchemaValidationResult ValidateComparisonExplanationJson(string json)
    {
        return new SchemaValidationResult();
    }

    public Task<SchemaValidationResult> ValidateAgentResultJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SchemaValidationResult());
    }

    public Task<SchemaValidationResult> ValidateGoldenManifestJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SchemaValidationResult());
    }
}
