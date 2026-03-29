using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Parsing;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Locks DI registration for the context-ingestion connector pipeline to the order defined in
/// <see cref="ArchiForge.ContextIngestion.Infrastructure.ContextConnectorPipeline.CreateOrderedContextConnectorPipeline"/>
/// and <see cref="ArchiForge.ContextIngestion.Infrastructure.ContextDocumentParserPipeline.CreateOrderedContextDocumentParsers"/>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ContextIngestionConnectorRegistrationTests(ArchiForgeApiFactory factory)
    : IClassFixture<ArchiForgeApiFactory>
{
    [Fact]
    public void Services_Resolve_IEnumerable_IContextConnector_InPipelineOrder()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IEnumerable<IContextConnector> connectors = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IContextConnector>>();

        Type[] types = connectors.Select(c => c.GetType()).ToArray();

        types.Should().Equal(
            typeof(StaticRequestContextConnector),
            typeof(InlineRequirementsConnector),
            typeof(DocumentConnector),
            typeof(PolicyReferenceConnector),
            typeof(TopologyHintsConnector),
            typeof(SecurityBaselineHintsConnector),
            typeof(InfrastructureDeclarationConnector));
    }

    [Fact]
    public void Services_Resolve_IReadOnlyList_IContextDocumentParser_InPipelineOrder()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IReadOnlyList<IContextDocumentParser> parsers = scope.ServiceProvider
            .GetRequiredService<IReadOnlyList<IContextDocumentParser>>();

        parsers.Select(p => p.GetType()).Should().Equal(typeof(PlainTextContextDocumentParser));
    }
}
