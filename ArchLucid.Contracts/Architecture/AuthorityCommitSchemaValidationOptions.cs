namespace ArchLucid.Contracts.Architecture;

/// <summary>Authority commit persistence guards (schema drift detection on the contract manifest).</summary>
public sealed class AuthorityCommitSchemaValidationOptions
{
    public const string SectionPath = "ArchLucid:AuthorityCommit";

    /// <summary>
    ///     When true (default), the authority commit orchestrator serializes the projected golden manifest contract
    ///     with canonical JSON settings and validates it against the shipped JSON Schema before calling finalization.
    /// </summary>
    public bool ValidateGoldenManifestSchema
    {
        get;
        set;
    } = true;
}
