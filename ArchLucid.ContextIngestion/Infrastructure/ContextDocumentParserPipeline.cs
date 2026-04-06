using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Parsing;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.ContextIngestion.Infrastructure;

/// <summary>
/// Canonical definition of <see cref="IContextDocumentParser"/> evaluation order inside
/// <see cref="Connectors.DocumentConnector"/>.
/// </summary>
/// <remarks>
/// When several parsers return true from <see cref="IContextDocumentParser.CanParse"/> for the same
/// content type, the first parser in this list wins. Register the ordered list only from
/// <see cref="CreateOrderedContextDocumentParsers"/> at the composition root (see API startup).
/// </remarks>
public static class ContextDocumentParserPipeline
{
    /// <summary>
    /// Builds the ordered parser list for DI. Call only from composition root registration.
    /// </summary>
    public static IReadOnlyList<IContextDocumentParser> CreateOrderedContextDocumentParsers(
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return
        [
            services.GetRequiredService<PlainTextContextDocumentParser>()
            // Future specialized parsers: insert here in explicit precedence order.
        ];
    }
}
