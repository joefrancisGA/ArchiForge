namespace ArchLucid.Application.Billing;

/// <summary>Configurable unit rates for rough tenant spend guidance (not invoice truth).</summary>
public sealed class BillingUnitRatesOptions
{
    public const string SectionPath = "Billing:UnitRates";

    public string Currency
    {
        get;
        set;
    } = "USD";

    public decimal StandardMonthlyUsdLow
    {
        get;
        set;
    } = 49;

    public decimal StandardMonthlyUsdHigh
    {
        get;
        set;
    } = 199;

    public decimal EnterpriseMonthlyUsdLow
    {
        get;
        set;
    } = 199;

    public decimal EnterpriseMonthlyUsdHigh
    {
        get;
        set;
    } = 899;

    public string MethodologyNote
    {
        get;
        set;
    } =
        "Heuristic band from configured list prices plus assumed LLM attach variance; reconcile against Azure Cost Management + Stripe/Marketplace invoices.";
}
