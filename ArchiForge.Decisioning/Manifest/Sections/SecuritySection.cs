namespace ArchiForge.Decisioning.Models;

public class SecuritySection
{
    public List<SecurityPostureItem> Controls { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
}

