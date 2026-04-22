namespace ArchLucid.Contracts.Common;

/// <summary>Classifies the functional role of a service within the architecture topology.</summary>
public enum ServiceType
{
    /// <summary>Unspecified — populate from knowledge-graph <c>Properties</c> or explicit design-time input.</summary>
    Unknown = 0,

    /// <summary>HTTP API or REST/GraphQL service.</summary>
    Api = 1,
    /// <summary>Background worker or queue processor.</summary>
    Worker = 2,
    /// <summary>User-interface or web front-end.</summary>
    Ui = 3,
    /// <summary>External integration or adapter service.</summary>
    Integration = 4,
    /// <summary>Internal data access or persistence service.</summary>
    DataService = 5,
    /// <summary>Full-text or semantic search service.</summary>
    SearchService = 6,
    /// <summary>AI inference or model-serving service.</summary>
    AiService = 7
}
