namespace ArchLucid.Api.Models.Tenancy;

/// <summary>Allowed values for self-service registration (server-side validation).</summary>
public static class StructuredBaselineConstants
{
    public static readonly string[] AllowedCompanySizes =
    [
        "1-10", "11-50", "51-200", "201-1000", "1001-5000", "5001-50000", "50001+"
    ];

    public static readonly string[] IndustryVerticals =
    [
        "Healthcare",
        "Financial Services",
        "Technology",
        "Government / Public Sector",
        "Manufacturing",
        "Retail",
        "Insurance",
        "Energy / Utilities",
        "Education",
        "Telecommunications",
        "Other"
    ];
}
