namespace ArchiForge.Decisioning.Manifest.Sections;

public class ComplianceSection
{
    public List<CompliancePostureItem> Controls { get; set; } = [];

    public List<string> Gaps { get; set; } = [];
}
