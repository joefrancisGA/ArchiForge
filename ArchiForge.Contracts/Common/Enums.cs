namespace ArchiForge.Contracts.Common;

/// <summary>Identifies the target cloud platform for an architecture run.</summary>
public enum CloudProvider
{
    /// <summary>Microsoft Azure.</summary>
    Azure = 1
}

/// <summary>Lifecycle state of an <see cref="ArchiForge.Contracts.Metadata.ArchitectureRun"/>.</summary>
public enum ArchitectureRunStatus
{
    /// <summary>Run record created; no tasks generated yet.</summary>
    Created = 1,
    /// <summary>Agent tasks have been generated and are ready for dispatch.</summary>
    TasksGenerated = 2,
    /// <summary>Tasks dispatched; waiting for all agent results to be submitted.</summary>
    WaitingForResults = 3,
    /// <summary>All results received; run is ready to be committed to a golden manifest.</summary>
    ReadyForCommit = 4,
    /// <summary>Golden manifest committed; run is complete and immutable.</summary>
    Committed = 5,
    /// <summary>Run failed during task execution or commit; see run error details.</summary>
    Failed = 6
}

/// <summary>Lifecycle state of an individual <c>AgentTask</c> within a run.</summary>
public enum AgentTaskStatus
{
    /// <summary>Task created and queued for dispatch.</summary>
    Created = 1,
    /// <summary>Task dispatched to an agent and currently executing.</summary>
    InProgress = 2,
    /// <summary>Agent submitted a valid result; task complete.</summary>
    Completed = 3,
    /// <summary>Agent submitted a result that was rejected by validation.</summary>
    Rejected = 4,
    /// <summary>Task failed during execution; see task error details.</summary>
    Failed = 5
}

/// <summary>Identifies the specialized role of an agent within the decision pipeline.</summary>
public enum AgentType
{
    /// <summary>Proposes service topology, relationships, and patterns.</summary>
    Topology = 1,
    /// <summary>Estimates and validates cost implications of the topology.</summary>
    Cost = 2,
    /// <summary>Evaluates governance and security-control compliance.</summary>
    Compliance = 3,
    /// <summary>Reviews and challenges proposals from other agents (peer evaluation).</summary>
    Critic = 4
}

/// <summary>Classifies the functional role of a service within the architecture topology.</summary>
public enum ServiceType
{
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

/// <summary>Azure runtime hosting platform assigned to a service or datastore in the manifest.</summary>
public enum RuntimePlatform
{
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

/// <summary>Classifies the storage technology used by a datastore in the architecture manifest.</summary>
public enum DatastoreType
{
    /// <summary>Relational (SQL) database.</summary>
    Sql = 1,
    /// <summary>Document or key-value store (NoSQL).</summary>
    NoSql = 2,
    /// <summary>Object / blob storage.</summary>
    Object = 3,
    /// <summary>In-memory or distributed cache.</summary>
    Cache = 4,
    /// <summary>Full-text or vector search index.</summary>
    Search = 5
}

/// <summary>Describes the nature of a directed relationship between two topology entities.</summary>
public enum RelationshipType
{
    /// <summary>Source service makes synchronous calls to the target.</summary>
    Calls = 1,
    /// <summary>Source reads data from the target store.</summary>
    ReadsFrom = 2,
    /// <summary>Source writes data to the target store.</summary>
    WritesTo = 3,
    /// <summary>Source publishes messages or events to the target.</summary>
    PublishesTo = 4,
    /// <summary>Source subscribes to messages or events from the target.</summary>
    SubscribesTo = 5,
    /// <summary>Source uses the target for authentication or token validation.</summary>
    AuthenticatesWith = 6
}
