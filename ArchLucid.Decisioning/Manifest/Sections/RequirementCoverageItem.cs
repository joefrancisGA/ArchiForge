namespace ArchLucid.Decisioning.Manifest.Sections;

public class RequirementCoverageItem
{
    public string RequirementName
    {
        get;
        set;
    } = null!;

    public string RequirementText
    {
        get;
        set;
    } = null!;

    public bool IsMandatory
    {
        get;
        set;
    }

    public string CoverageStatus
    {
        get;
        set;
    } = null!;

    public List<string> SupportingFindingIds
    {
        get;
        set;
    } = [];
}
