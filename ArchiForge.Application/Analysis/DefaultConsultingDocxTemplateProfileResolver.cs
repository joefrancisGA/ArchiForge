using System.Collections.Generic;

namespace ArchiForge.Application.Analysis;

public sealed class DefaultConsultingDocxTemplateProfileResolver : IConsultingDocxTemplateProfileResolver
{
    public ConsultingDocxTemplateProfileCatalog GetCatalog()
    {
        return new ConsultingDocxTemplateProfileCatalog
        {
            Profiles =
            [
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = "executive",
                    ProfileDisplayName = "Executive Brief",
                    DisplayOrder = 10
                },
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = "regulated",
                    ProfileDisplayName = "Regulated / Compliance Review",
                    DisplayOrder = 20
                },
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = "client",
                    ProfileDisplayName = "Client Delivery Report",
                    DisplayOrder = 30
                },
                new ConsultingDocxTemplateProfileInfo
                {
                    ProfileName = "internal",
                    ProfileDisplayName = "Internal Technical Review",
                    DisplayOrder = 40
                }
            ]
        };
    }
}

