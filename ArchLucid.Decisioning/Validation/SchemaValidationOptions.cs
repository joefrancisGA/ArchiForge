namespace ArchLucid.Decisioning.Validation;

public sealed class SchemaValidationOptions
{
    public const string SectionName = "SchemaValidation";

    public string AgentResultSchemaPath
    {
        get;
        set;
    } = "schemas/agentresult.schema.json";

    public string GoldenManifestSchemaPath
    {
        get;
        set;
    } = "schemas/goldenmanifest.schema.json";

    public string ExplanationRunSchemaPath
    {
        get;
        set;
    } = "schemas/explanation-run.schema.json";

    public string ComparisonExplanationSchemaPath
    {
        get;
        set;
    } = "schemas/comparison-explanation.schema.json";

    public bool EnableDetailedErrors
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     When true, validation results are cached by a SHA-256 hash of the JSON payload.
    ///     Identical payloads skip re-evaluation. Default false to avoid stale results during testing.
    /// </summary>
    public bool EnableResultCaching
    {
        get;
        set;
    } = false;

    /// <summary>Maximum number of cached validation results before the cache is cleared. Default 256.</summary>
    public int ResultCacheMaxSize
    {
        get;
        set;
    } = 256;
}
