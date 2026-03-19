namespace ArchiForge.DecisionEngine.Validation;

/// <summary>
/// <see cref="ISchemaValidationService"/> that always succeeds. Use in tests that exercise merge logic without JSON schema files.
/// </summary>
public sealed class PassthroughSchemaValidationService : ISchemaValidationService
{
    public SchemaValidationResult ValidateAgentResultJson(string json) => new();

    public SchemaValidationResult ValidateGoldenManifestJson(string json) => new();

    public Task<SchemaValidationResult> ValidateAgentResultJsonAsync(
        string json,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchemaValidationResult());

    public Task<SchemaValidationResult> ValidateGoldenManifestJsonAsync(
        string json,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchemaValidationResult());
}
