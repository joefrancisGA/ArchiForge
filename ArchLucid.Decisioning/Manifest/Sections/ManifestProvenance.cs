namespace ArchLucid.Decisioning.Manifest.Sections;

public class ManifestProvenance
{
    public List<string> SourceFindingIds
    {
        get;
        set;
    } = [];

    public List<string> SourceGraphNodeIds
    {
        get;
        set;
    } = [];

    public List<string> AppliedRuleIds
    {
        get;
        set;
    } = [];
}
