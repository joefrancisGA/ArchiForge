namespace ArchLucid.Cli;

internal sealed class ManifestValidateError
{
    public required string Message
    {
        get;
        init;
    }

    public int? LineNumber
    {
        get;
        init;
    }

    public int? Column
    {
        get;
        init;
    }

    public string? InstancePointer
    {
        get;
        init;
    }
}
