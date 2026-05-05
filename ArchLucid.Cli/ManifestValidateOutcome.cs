namespace ArchLucid.Cli;

internal sealed class ManifestValidateOutcome
{
    public List<ManifestValidateError> Errors { get; } = [];

    public bool IsValid => Errors.Count == 0;
}
