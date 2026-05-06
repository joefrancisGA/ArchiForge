using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Analysis;
/// <summary>
///     Default implementation of <see cref = "IConsultingDocxTemplateOptionsProvider"/> that
///     reads options from the ASP.NET Core <see cref = "IOptions{TOptions}"/> pipeline.
/// </summary>
public sealed class DefaultConsultingDocxTemplateOptionsProvider(IOptions<ConsultingDocxTemplateOptions> options) : IConsultingDocxTemplateOptionsProvider
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(options);
    private static byte __ValidatePrimaryConstructorArguments(Microsoft.Extensions.Options.IOptions<ArchLucid.Application.Analysis.ConsultingDocxTemplateOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return (byte)0;
    }

    /// <inheritdoc/>
    public ConsultingDocxTemplateOptions GetOptions()
    {
        return options.Value;
    }
}