namespace ArchLucid.Decisioning.Findings.Payloads;

public class RequirementCoverageFindingPayload
{
    public int RequirementNodeCount
    {
        get;
        set;
    }

    public int CoveredRequirementCount
    {
        get;
        set;
    }

    public int UncoveredRequirementCount
    {
        get;
        set;
    }

    public List<string> UncoveredRequirements
    {
        get;
        set;
    } = [];
}
