using System.ComponentModel.DataAnnotations;

namespace ArchLucid.Api.Models.Tenancy;

/// <summary>Public self-service tenant registration (Free tier).</summary>
public sealed class TenantRegistrationRequest
{
    [Required]
    [MaxLength(200)]
    public string OrganizationName
    {
        get;
        init;
    } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string AdminEmail
    {
        get;
        init;
    } = string.Empty;

    [MaxLength(200)]
    public string? AdminDisplayName
    {
        get;
        init;
    }

    /// <summary>Optional: median hours from architecture request to reviewable package (current state).</summary>
    public decimal? BaselineReviewCycleHours
    {
        get;
        init;
    }

    /// <summary>Optional short provenance (e.g. team estimate); max 256 after trim.</summary>
    public string? BaselineReviewCycleSource
    {
        get;
        init;
    }

    [MaxLength(30)]
    public string? CompanySize
    {
        get;
        init;
    }

    public int? ArchitectureTeamSize
    {
        get;
        init;
    }

    [MaxLength(100)]
    public string? IndustryVertical
    {
        get;
        init;
    }

    [MaxLength(200)]
    public string? IndustryVerticalOther
    {
        get;
        init;
    }
}
