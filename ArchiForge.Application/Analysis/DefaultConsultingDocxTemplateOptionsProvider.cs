using Microsoft.Extensions.Options;

namespace ArchiForge.Application.Analysis;

public sealed class DefaultConsultingDocxTemplateOptionsProvider(IOptions<ConsultingDocxTemplateOptions> options)
    : IConsultingDocxTemplateOptionsProvider
{
    public ConsultingDocxTemplateOptions GetOptions()
    {
        return options.Value;
    }
}

