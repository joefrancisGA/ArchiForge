namespace ArchLucid.Decisioning.Manifest.Sections;

public class ManifestMetadata
{
    public string Name
    {
        get;
        set;
    } = "ArchLucid Manifest";

    public string Version
    {
        get;
        set;
    } = "1.0.0";

    public string Status
    {
        get;
        set;
    } = "Draft";

    public string Summary
    {
        get;
        set;
    } = string.Empty;
}
