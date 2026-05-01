namespace ArchLucid.Application.Analysis;

/// <summary>
///     Built-in implementation of <see cref="IConsultingDocxTemplateProfileResolver" /> that
///     returns the four standard consulting template profiles.
/// </summary>
/// <remarks>
///     Profile names match the constants in <see cref="ConsultingDocxProfiles" />. Replace or
///     extend this resolver via DI when custom profiles are needed.
/// </remarks>
public sealed class DefaultConsultingDocxTemplateProfileResolver : IConsultingDocxTemplateProfileResolver
{
    /// <inheritdoc />
    public ConsultingDocxTemplateProfileCatalog GetCatalog()
    {
        return new ConsultingDocxTemplateProfileCatalog
        {
            Profiles =
            [
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = ConsultingDocxProfiles.Executive,
                    ProfileDisplayName = "Executive Brief",
                    DisplayOrder = 10
                },
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = ConsultingDocxProfiles.Regulated,
                    ProfileDisplayName = "Regulated / Compliance Review",
                    DisplayOrder = 20
                },
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = ConsultingDocxProfiles.Client,
                    ProfileDisplayName = "Client Delivery Report",
                    DisplayOrder = 30
                },
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = ConsultingDocxProfiles.Internal,
                    ProfileDisplayName = "Internal Technical Review",
                    DisplayOrder = 40
                }
            ]
        };
    }
}
