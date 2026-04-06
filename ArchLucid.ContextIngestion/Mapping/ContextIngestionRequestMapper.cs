using ArchiForge.ContextIngestion.Models;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.ContextIngestion.Mapping;

/// <summary>
/// Maps API / coordinator <see cref="ArchitectureRequest"/> into the ingestion pipeline model.
/// <see cref="ArchitectureRequest.SystemName"/> becomes <see cref="ContextIngestionRequest.ProjectId"/>.
/// </summary>
public static class ContextIngestionRequestMapper
{
    public static ContextIngestionRequest FromArchitectureRequest(ArchitectureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ContextIngestionRequest
        {
            ProjectId = request.SystemName,
            Description = request.Description,
            InlineRequirements = request.InlineRequirements.ToList(),
            Documents = request.Documents
                .Select(d => new ContextDocumentReference
                {
                    Name = d.Name,
                    ContentType = d.ContentType,
                    Content = d.Content
                })
                .ToList(),
            PolicyReferences = request.PolicyReferences.ToList(),
            TopologyHints = request.TopologyHints.ToList(),
            SecurityBaselineHints = request.SecurityBaselineHints.ToList(),
            InfrastructureDeclarations = request.InfrastructureDeclarations
                .Select(x => new InfrastructureDeclarationReference
                {
                    Name = x.Name,
                    Format = x.Format,
                    Content = x.Content
                })
                .ToList()
        };
    }
}
