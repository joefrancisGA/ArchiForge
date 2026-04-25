namespace ArchLucid.Api.Tests;

public sealed class ConsultingDocxProfileRecommendationDto
{
    public string RecommendedProfileName
    {
        get;
        set;
    } = string.Empty;

    public string RecommendedProfileDisplayName
    {
        get;
        set;
    } = string.Empty;

    public string Reason
    {
        get;
        set;
    } = string.Empty;

    public List<string> AlternativeProfiles
    {
        get;
        set;
    } = [];
}
