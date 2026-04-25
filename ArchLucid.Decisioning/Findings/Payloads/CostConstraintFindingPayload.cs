namespace ArchLucid.Decisioning.Findings.Payloads;

public class CostConstraintFindingPayload
{
    public string BudgetName
    {
        get;
        set;
    } = null!;

    public decimal? MaxMonthlyCost
    {
        get;
        set;
    }

    public string CostRisk
    {
        get;
        set;
    } = null!;
}
