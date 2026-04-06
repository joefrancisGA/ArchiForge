namespace ArchiForge.Decisioning.Manifest.Sections;

public class SecuritySection
{
    public List<SecurityPostureItem> Controls { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
}

