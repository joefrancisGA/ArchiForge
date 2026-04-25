namespace ArchLucid.Decisioning.Manifest.Sections;

public class ConstraintSection
{
    public List<string> MandatoryConstraints
    {
        get;
        set;
    } = [];

    public List<string> Preferences
    {
        get;
        set;
    } = [];
}
