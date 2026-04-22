namespace ArchLucid.Contracts.Common;

/// <summary>Azure runtime hosting platform assigned to a service or datastore in the manifest.</summary>
public enum RuntimePlatform
{
    /// <summary>Unspecified — populate from knowledge-graph <c>Properties</c> or explicit design-time input.</summary>
    Unknown = 0,

    /// <summary>Azure App Service (PaaS web/API hosting).</summary>
    AppService = 1,
    /// <summary>Azure Functions (serverless compute).</summary>
    Functions = 2,
    /// <summary>Azure Kubernetes Service.</summary>
    Aks = 3,
    /// <summary>Azure Virtual Machine.</summary>
    Vm = 4,
    /// <summary>Azure Container Apps (serverless containers).</summary>
    ContainerApps = 5,
    /// <summary>Azure SQL Database / SQL Managed Instance.</summary>
    SqlServer = 6,
    /// <summary>Azure AI Search (formerly Cognitive Search).</summary>
    AzureAiSearch = 7,
    /// <summary>Azure OpenAI Service.</summary>
    AzureOpenAi = 8,
    /// <summary>Azure Cache for Redis.</summary>
    Redis = 9,
    /// <summary>Azure Blob Storage.</summary>
    BlobStorage = 10,
    /// <summary>Azure Key Vault.</summary>
    KeyVault = 11
}
