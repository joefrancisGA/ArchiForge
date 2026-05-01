using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Default implementation of <see cref="IConsultingDocxTemplateOptionsProvider" /> that
///     reads options from the ASP.NET Core <see cref="IOptions{TOptions}" /> pipeline.
/// </summary>
public sealed class DefaultConsultingDocxTemplateOptionsProvider(IOptions<ConsultingDocxTemplateOptions> options)
    : IConsultingDocxTemplateOptionsProvider
{
    /// <inheritdoc />
    public ConsultingDocxTemplateOptions GetOptions()
    {
        return options.Value;
    }
}
