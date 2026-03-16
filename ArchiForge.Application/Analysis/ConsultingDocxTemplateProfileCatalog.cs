using System.Collections.Generic;

namespace ArchiForge.Application.Analysis;

public sealed class ConsultingDocxTemplateProfileCatalog
{
    public List<ConsultingDocxTemplateProfileInfo> Profiles { get; set; } = [];
}

public sealed class ConsultingDocxTemplateProfileInfo
{
    public string ProfileName { get; set; } = string.Empty;

    public string ProfileDisplayName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 100;
}

