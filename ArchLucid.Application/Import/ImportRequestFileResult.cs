namespace ArchLucid.Application.Import;

/// <summary>Outcome of <see cref="IImportRequestFileService.ImportAsync" />.</summary>
public sealed class ImportRequestFileResult
{
    public Guid ImportedRequestId
    {
        get;
        init;
    }

    public string Status
    {
        get;
        init;
    } = "Draft";

    public IReadOnlyList<string> Warnings
    {
        get;
        init;
    } = [];
}
